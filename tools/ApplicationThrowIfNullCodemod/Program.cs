using System.Collections.Immutable;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace ApplicationThrowIfNullCodemod;

internal static class Program
{
    private static readonly SymbolDisplayFormat NullableFqReturnFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                             SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                  SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static SymbolDisplayFormat NullableFullyQualifiedReturnFormat => NullableFqReturnFormat;

    private static readonly SymbolDisplayFormat PrimaryCtorParameterDisplayFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                             SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                  SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static SymbolDisplayFormat PrimaryCtorParameterDisplay => PrimaryCtorParameterDisplayFormat;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length is 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Usage: ApplicationThrowIfNullCodemod <path-to-ArchLucid.Application.csproj>");
            return 1;
        }

        string projectPath = Path.GetFullPath(args[0]);
        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Project file not found: {projectPath}");
            return 1;
        }

        MSBuildLocator.RegisterDefaults();
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        Project project = await workspace.OpenProjectAsync(projectPath);
        project = workspace.CurrentSolution.GetProject(project.Id) ?? project;

        Compilation? compilation = await project.GetCompilationAsync();
        if (compilation is null)
        {
            Console.Error.WriteLine("Failed to compile project.");
            return 2;
        }

        ImmutableArray<Diagnostic> failures =
            compilation.GetDiagnostics(default).Where(IsBlocking).ToImmutableArray();
        if (!failures.IsEmpty)
        {
            Console.Error.WriteLine($"Compilation has {failures.Length} blocking error(s):");
            foreach (Diagnostic diag in failures)
                Console.Error.WriteLine(diag);
            return 3;
        }

        Solution solution = workspace.CurrentSolution;
        AdhocWorkspace formatWorkspace = new();
        int editedDocuments = 0;

        foreach (DocumentId docId in project.DocumentIds)
        {
            Document doc = solution.GetDocument(docId) ?? project.GetDocument(docId);
            if (doc is null || doc.FilePath is null)
                continue;

            SyntaxTree? syntaxTreeNullable = await doc.GetSyntaxTreeAsync();
            if (syntaxTreeNullable is null)
                continue;

            SyntaxTree tree = syntaxTreeNullable;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            CompilationUnitSyntax root =
                await tree.GetRootAsync() as CompilationUnitSyntax ?? throw new InvalidOperationException();

            ThrowIfNullRewriter rewriter = new(semanticModel);
            CompilationUnitSyntax rewrittenRoot = rewriter.Visit(root) as CompilationUnitSyntax ?? root;

            RemoveAnnotatedNullableThrowIfNullRewriter cleanup = new(semanticModel);
            CompilationUnitSyntax finalRoot =
                cleanup.Visit(rewrittenRoot) as CompilationUnitSyntax ?? rewrittenRoot;

            if (!rewriter.MadeChanges && !cleanup.MadeChanges)
                continue;

            string originalText =
                SourceText.From(await File.ReadAllTextAsync(doc.FilePath)).ToString();
            SyntaxNode formatted = Formatter.Format(finalRoot, formatWorkspace).NormalizeWhitespace();
            string formattedText = formatted.ToFullString();
            if (string.Equals(formattedText, originalText, StringComparison.Ordinal))
                continue;

            UTF8Encoding utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
            await File.WriteAllTextAsync(doc.FilePath, formattedText, utf8WithoutBom);
            editedDocuments++;
        }

        Console.WriteLine($"Edited {editedDocuments} document(s).");
        return 0;
    }

    private static bool IsBlocking(Diagnostic d) =>
        d.Severity == DiagnosticSeverity.Error
        && d.Id is not "CS8032" and not "CS1704" and not "CS0436";
}

internal sealed class ThrowIfNullRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;

    public bool MadeChanges { get; private set; }

    public ThrowIfNullRewriter(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        INamedTypeSymbol? typeSymbolOriginal = _semanticModel.GetDeclaredSymbol(node);

        ClassDeclarationSyntax visited =
            (ClassDeclarationSyntax)(base.VisitClassDeclaration(node) ?? node)!;

        if (!visited.Modifiers.Any(SyntaxKind.PublicKeyword))
            return visited;

        if (!IsEligiblePublicClassOrRecordType(typeSymbolOriginal))
            return visited;

        return MaybeInjectPrimaryConstructorValidation(visited, node.ParameterList);
    }

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (node.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
            return base.VisitRecordDeclaration(node);

        INamedTypeSymbol? typeSymbolOriginal = _semanticModel.GetDeclaredSymbol(node);

        RecordDeclarationSyntax visited =
            (RecordDeclarationSyntax)(base.VisitRecordDeclaration(node) ?? node)!;

        if (!visited.Modifiers.Any(SyntaxKind.PublicKeyword))
            return visited;

        if (!IsEligiblePublicClassOrRecordType(typeSymbolOriginal))
            return visited;

        return MaybeInjectPrimaryConstructorValidationOnRecord(visited, node.ParameterList);
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        IMethodSymbol? symbol = _semanticModel.GetDeclaredSymbol(node);

        MethodDeclarationSyntax inner =
            (MethodDeclarationSyntax)(base.VisitMethodDeclaration(node) ?? node)!;

        if (!IsEligiblePublicDeclaringType(symbol?.ContainingType))
            return inner;

        if (symbol is null || symbol.DeclaredAccessibility != Accessibility.Public)
            return inner;

        if (symbol.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.Constructor)
            return inner;

        if (symbol.IsStatic &&
            string.Equals(symbol.Name, "__ValidatePrimaryConstructorArguments", StringComparison.Ordinal))
            return inner;

        inner = MaybeAnnotateReturnType(inner, symbol);
        return MaybeInsertParameterGuards(inner, symbol);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        IMethodSymbol? symbol = _semanticModel.GetDeclaredSymbol(node);

        ConstructorDeclarationSyntax inner =
            (ConstructorDeclarationSyntax)(base.VisitConstructorDeclaration(node) ?? node)!;

        if (!IsEligiblePublicDeclaringType(symbol?.ContainingType))
            return inner;

        if (symbol is null || symbol.DeclaredAccessibility != Accessibility.Public)
            return inner;

        return MaybeInsertParameterGuardsCtor(inner, symbol);
    }

    private static bool IsEligiblePublicClassOrRecordType(INamedTypeSymbol? typeSymbol)
    {
        return typeSymbol is { DeclaredAccessibility: Accessibility.Public, IsValueType: false } ts &&
               (ts.IsRecord || ts.TypeKind == TypeKind.Class);
    }

    private static bool IsEligiblePublicDeclaringType(INamedTypeSymbol? containingType) =>
        IsEligiblePublicClassOrRecordType(containingType);

    private ClassDeclarationSyntax MaybeInjectPrimaryConstructorValidation(
        ClassDeclarationSyntax visited,
        ParameterListSyntax? plistOriginal)
    {
        if (plistOriginal is null || plistOriginal.Parameters.Count is 0)
            return visited;

        if (visited.Members.OfType<MethodDeclarationSyntax>().Any(static m =>
                string.Equals(m.Identifier.Text, "__ValidatePrimaryConstructorArguments", StringComparison.Ordinal)))
            return visited;

        ImmutableArray<IParameterSymbol> refParams =
            GetReferenceParametersNeedingCheck(plistOriginal);
        if (refParams.IsEmpty)
            return visited;

        string argList = string.Join(", ", refParams.Select(static p => p.Name));
        string paramDecl =
            string.Join(", ", refParams.Select(static p =>
                $"{p.Type.ToDisplayString(Program.PrimaryCtorParameterDisplay)} {p.Name}"));

        string fieldAndMethod =
            $$"""
            private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments({{argList}});
            private static byte __ValidatePrimaryConstructorArguments({{paramDecl}})
            {
            {{string.Join(Environment.NewLine, refParams.Select(static p => $"    ArgumentNullException.ThrowIfNull({p.Name});"))}}
                return (byte)0;
            }

            """;

        MemberDeclarationSyntax[] parsed = ParseMembers(fieldAndMethod);
        SyntaxList<MemberDeclarationSyntax> newMembers = visited.Members.InsertRange(0, parsed);
        MadeChanges = true;
        return visited.WithMembers(newMembers);
    }

    private RecordDeclarationSyntax MaybeInjectPrimaryConstructorValidationOnRecord(
        RecordDeclarationSyntax visited,
        ParameterListSyntax? plistOriginal)
    {
        if (plistOriginal is null || plistOriginal.Parameters.Count is 0)
            return visited;

        if (visited.Members.OfType<MethodDeclarationSyntax>().Any(static m =>
                string.Equals(m.Identifier.Text, "__ValidatePrimaryConstructorArguments", StringComparison.Ordinal)))
            return visited;

        ImmutableArray<IParameterSymbol> refParams =
            GetReferenceParametersNeedingCheck(plistOriginal);
        if (refParams.IsEmpty)
            return visited;

        RecordDeclarationSyntax opened = EnsureSemicolonPrimaryRecordHasBraces(visited);

        string argList =
            string.Join(", ", refParams.Select(static p => p.Name));
        string paramDecl =
            string.Join(", ", refParams.Select(static p =>
                $"{p.Type.ToDisplayString(Program.PrimaryCtorParameterDisplay)} {p.Name}"));

        string fieldAndMethod =
            $$"""
            private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments({{argList}});
            private static byte __ValidatePrimaryConstructorArguments({{paramDecl}})
            {
            {{string.Join(Environment.NewLine, refParams.Select(static p =>
                $"        ArgumentNullException.ThrowIfNull({p.Name});"))}}
                return (byte)0;
            }

            """;

        MemberDeclarationSyntax[] parsed = ParseMembers(fieldAndMethod);
        SyntaxList<MemberDeclarationSyntax> newMembers =
            opened.Members.InsertRange(0, parsed);
        MadeChanges = true;

        return opened.WithMembers(newMembers);
    }

    private static RecordDeclarationSyntax EnsureSemicolonPrimaryRecordHasBraces(
        RecordDeclarationSyntax recordDeclaration)
    {
        if (!recordDeclaration.OpenBraceToken.IsKind(SyntaxKind.None))
            return recordDeclaration;

        if (!recordDeclaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            return recordDeclaration;

        SyntaxTriviaList semicolonTrailing =
            recordDeclaration.SemicolonToken.TrailingTrivia;
        SyntaxToken closingBrace =
            SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(semicolonTrailing);

        return recordDeclaration
            .WithSemicolonToken(default)
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(closingBrace);
    }

    private static MemberDeclarationSyntax[] ParseMembers(string text)
    {
        string wrapped = "class __Tmp { " + text + " }";
        CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(
                wrapped,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))
            .GetCompilationUnitRoot();
        ClassDeclarationSyntax tmp =
            unit.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        return tmp.Members.ToArray();
    }

    private ImmutableArray<IParameterSymbol>
        GetReferenceParametersNeedingCheck(ParameterListSyntax plist)
    {
        ImmutableArray<IParameterSymbol>.Builder builder =
            ImmutableArray.CreateBuilder<IParameterSymbol>();

        foreach (ParameterSyntax ps in plist.Parameters)
        {
            IParameterSymbol? p = _semanticModel.GetDeclaredSymbol(ps);
            if (p is null || p.RefKind == RefKind.Out)
                continue;

            if (!RequiresNonNullableReferenceThrowIfNull(p))
                continue;

            builder.Add(p);
        }

        return builder.ToImmutable();
    }

    private static bool RequiresNonNullableReferenceThrowIfNull(IParameterSymbol p)
    {
        if (p.RefKind == RefKind.Out)
            return false;

        if (p.NullableAnnotation != NullableAnnotation.NotAnnotated)
            return false;

        ITypeSymbol t = p.Type;

        return t switch
        {
            ITypeParameterSymbol tp =>
                tp.HasReferenceTypeConstraint,
            _ =>
                t.IsReferenceType,
        };
    }

    private MethodDeclarationSyntax MaybeAnnotateReturnType(MethodDeclarationSyntax node, IMethodSymbol symbol)
    {
        if (symbol.ReturnsVoid)
            return node;

        if (!RequiresExplicitNullableReturnAnnotation(symbol))
            return node;

        if (node.Body is null && node.ExpressionBody is null)
            return node;

        TypeSyntax priorReturnSyntax = node.ReturnType;

        string display =
            symbol.ReturnType.ToDisplayString(Program.NullableFullyQualifiedReturnFormat);
        TypeSyntax reparsed =
            SyntaxFactory.ParseTypeName(display)
                .WithLeadingTrivia(priorReturnSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(priorReturnSyntax.GetTrailingTrivia());

        if (EquivalentReturnTypeIgnoringTrivia(node.ReturnType, reparsed))
            return node;

        MadeChanges = true;

        return node.WithReturnType(reparsed);
    }

    private static bool EquivalentReturnTypeIgnoringTrivia(TypeSyntax a, TypeSyntax b)
    {
        return string.Equals(Normalized(a), Normalized(b), StringComparison.Ordinal);

        static string Normalized(TypeSyntax s) =>
            s.NormalizeWhitespace().WithoutTrivia().ToFullString();
    }

    private static bool RequiresExplicitNullableReturnAnnotation(IMethodSymbol method)
    {
        if (method.ReturnsVoid)
            return false;

        return HasNullableAnnotatedReferenceReturn(method);
    }

    private static bool HasNullableAnnotatedReferenceReturn(IMethodSymbol method)
    {
        ITypeSymbol rt = method.ReturnType;
        if (method.ReturnNullableAnnotation == NullableAnnotation.Annotated &&
            rt.IsReferenceType)
            return true;

        INamedTypeSymbol? named = rt as INamedTypeSymbol;
        if (named is null || named.TypeArguments.Length is 0)
            return false;

        if (!IsConstructedTaskOrValueTask(named))
            return false;

        ITypeSymbol inner =
            named.TypeArguments[0];
        NullableAnnotation innerAnn =
            named.TypeArgumentNullableAnnotations[0];
        return innerAnn == NullableAnnotation.Annotated &&
               inner.IsReferenceType;
    }

    private static bool IsConstructedTaskOrValueTask(INamedTypeSymbol named)
    {
        string def =
            named.OriginalDefinition.ToDisplayString();

        return string.Equals(def,
                   "System.Threading.Tasks.Task<TResult>",
                   StringComparison.Ordinal) ||
               string.Equals(def,
                   "System.Threading.Tasks.ValueTask<TResult>",
                   StringComparison.Ordinal);
    }

    private MethodDeclarationSyntax MaybeInsertParameterGuards(MethodDeclarationSyntax node,
        IMethodSymbol symbol)
    {
        if (symbol.IsAbstract)
            return node;

        BlockSyntax? block = node.Body;
        ExpressionSyntax? expressionBodyExpr = node.ExpressionBody?.Expression;

        if (block is null && expressionBodyExpr is not null)
            block = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expressionBodyExpr));

        if (block is null)
            return node;

        ImmutableArray<StatementSyntax> guards =
            BuildGuardStatements(symbol.Parameters, block.Statements);
        if (guards.IsEmpty)
            return node;

        SyntaxList<StatementSyntax> merged =
            InsertGuards(block.Statements, guards);
        BlockSyntax newBlock =
            block.WithStatements(merged).WithTrailingTrivia(block.GetTrailingTrivia());

        if (node.Body is null)
        {
            MadeChanges = true;

            return node.WithBody(newBlock).WithExpressionBody(null).WithSemicolonToken(default);
        }

        MadeChanges = true;

        return node.WithBody(newBlock);
    }

    private ConstructorDeclarationSyntax MaybeInsertParameterGuardsCtor(
        ConstructorDeclarationSyntax node,
        IMethodSymbol symbol)
    {
        BlockSyntax? block = node.Body;
        if (block is null)
            return node;

        ImmutableArray<StatementSyntax> guards =
            BuildGuardStatements(symbol.Parameters, block.Statements);
        if (guards.IsEmpty)
            return node;

        SyntaxList<StatementSyntax> merged =
            InsertGuards(block.Statements, guards);
        MadeChanges = true;

        return node.WithBody(block.WithStatements(merged));
    }

    private static ImmutableArray<StatementSyntax>
        BuildGuardStatements(ImmutableArray<IParameterSymbol> parameters,
            SyntaxList<StatementSyntax> existingStatements)
    {
        ImmutableArray<StatementSyntax>.Builder builder =
            ImmutableArray.CreateBuilder<StatementSyntax>();

        foreach (IParameterSymbol p in parameters)
        {
            if (p.RefKind == RefKind.Out)
                continue;

            if (!RequiresNonNullableReferenceThrowIfNull(p))
                continue;

            if (HasExistingNullGuard(p.Name, existingStatements))
                continue;

            InvocationExpressionSyntax invocation =
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseTypeName("ArgumentNullException"),
                            SyntaxFactory.IdentifierName(nameof(ArgumentNullException.ThrowIfNull))))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(p.Name)))));

            builder.Add(SyntaxFactory.ExpressionStatement(invocation));
        }

        return builder.ToImmutable();
    }

    private static SyntaxList<StatementSyntax> InsertGuards(
        SyntaxList<StatementSyntax> original, ImmutableArray<StatementSyntax> guards)
    {
        int insertAt = 0;

        while (insertAt < original.Count && original[insertAt] is LocalFunctionStatementSyntax)
            insertAt++;

        return original.InsertRange(insertAt, guards);
    }

    private static bool HasExistingNullGuard(string parameterName, SyntaxList<StatementSyntax> statements)
    {
        int upper = Math.Min(statements.Count, 40);

        for (int i = 0; i < upper; i++)
        {
            if (statements[i] is not ExpressionStatementSyntax es)
                continue;

            if (es.Expression is not InvocationExpressionSyntax inv)
                continue;

            InvocationExpressionSyntax peeled = PeelInvocation(inv);

            ExpressionSyntax callee = StripParentheses(peeled.Expression);

            if (callee is not MemberAccessExpressionSyntax ma)
                continue;

            if (ma.Name.Identifier.Text is not ("ThrowIfNull"
                or "ThrowIfNullOrWhiteSpace"))
                continue;

            if (peeled.ArgumentList.Arguments.Count is 0)
                continue;

            ArgumentSyntax arg0 =
                peeled.ArgumentList.Arguments[0];

            if (arg0.Expression is IdentifierNameSyntax id && id.Identifier.Text == parameterName)
                return true;
        }

        return false;

        static InvocationExpressionSyntax PeelInvocation(InvocationExpressionSyntax inv)
        {
            if (inv.Expression is ConditionalAccessExpressionSyntax conditional &&
                conditional.WhenNotNull is InvocationExpressionSyntax inner)
                return inner;

            return inv;
        }

        static ExpressionSyntax StripParentheses(ExpressionSyntax expr)
        {
            while (expr is ParenthesizedExpressionSyntax parentheses)
                expr = parentheses.Expression;

            return expr;
        }
    }
}

/// <summary>
/// Removes <see cref="ArgumentNullException.ThrowIfNull"/> statements that operate on parameters
/// whose nullability annotation is <see cref="NullableAnnotation.Annotated"/> (for example
/// <c>string?</c> optional parameters), matching the project's nullable reference semantics.
/// </summary>
internal sealed class RemoveAnnotatedNullableThrowIfNullRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;

    public bool MadeChanges { get; private set; }

    public RemoveAnnotatedNullableThrowIfNullRewriter(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax? node)
    {
        if (node is null)
            return null;

        if (node.Expression is not InvocationExpressionSyntax inv)
            return base.VisitExpressionStatement(node);

        if (!IsArgumentNullThrowIfNull(inv))
            return base.VisitExpressionStatement(node);

        if (inv.ArgumentList.Arguments.Count is 0)
            return base.VisitExpressionStatement(node);

        ArgumentSyntax firstArg = inv.ArgumentList.Arguments[0];
        if (firstArg.Expression is not IdentifierNameSyntax idName)
            return base.VisitExpressionStatement(node);

        ISymbol? enclosing = _semanticModel.GetEnclosingSymbol(node.SpanStart);

        IMethodSymbol? method = enclosing as IMethodSymbol;
        if (method is null && enclosing is not null)
            method = enclosing.ContainingSymbol as IMethodSymbol;

        if (method is null)
            return base.VisitExpressionStatement(node);

        IParameterSymbol? param = method.Parameters
            .SingleOrDefault(p => p.Name == idName.Identifier.ValueText);

        if (param is null)
            return base.VisitExpressionStatement(node);

        if (param.NullableAnnotation != NullableAnnotation.Annotated)
            return base.VisitExpressionStatement(node);

        MadeChanges = true;
        return null;
    }

    private static bool IsArgumentNullThrowIfNull(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Expression is not IdentifierNameSyntax typeName)
            return false;

        if (!string.Equals(typeName.Identifier.Text, nameof(ArgumentNullException),
                StringComparison.Ordinal))
            return false;

        return string.Equals(memberAccess.Name.Identifier.Text,
            nameof(ArgumentNullException.ThrowIfNull), StringComparison.Ordinal);
    }
}
