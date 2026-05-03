using System.Text;

using ArchLucid.Core.Scim.Filtering;

namespace ArchLucid.Application.Scim.Filtering;

/// <summary>Hand-rolled SCIM v2 filter parser (RFC 7644 §3.4.2.2) for flat user attributes only.</summary>
public static class ScimFilterParser
{
    public static ScimFilterNode? Parse(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return null;

        ScimFilterCursor c = new(filter.Trim());
        ScimFilterNode node = c.ParseFilter();
        c.SkipWs();

        return !c.Eof
            ? throw new ScimFilterParseException($"Unexpected trailing input at position {c.Position}.")
            : node;
    }

    private ref struct ScimFilterCursor
    {
        private readonly ReadOnlySpan<char> _s;

        public ScimFilterCursor(ReadOnlySpan<char> s)
        {
            _s = s;
            Position = 0;
        }

        public int Position
        {
            get;
            private set;
        }

        public bool Eof => Position >= _s.Length;

        public ScimFilterNode ParseFilter()
        {
            SkipWs();

            if (PeekKeyword("not"))
            {
                ConsumeKeyword("not");
                SkipWs();

                if (!TryConsume('('))
                    throw new ScimFilterParseException("Expected '(' after 'not'.");

                ScimFilterNode inner = ParseFilter();

                return !TryConsume(')')
                    ? throw new ScimFilterParseException("Expected ')' to close 'not'.")
                    : new ScimNotNode(inner);
            }

            ScimFilterNode left = ParseTerm();
            SkipWs();

            while (true)
            {
                if (PeekKeyword("and"))
                {
                    ConsumeKeyword("and");
                    SkipWs();
                    ScimFilterNode right = ParseTerm();
                    left = new ScimAndNode(left, right);
                    SkipWs();

                    continue;
                }

                if (PeekKeyword("or"))
                {
                    ConsumeKeyword("or");
                    SkipWs();
                    ScimFilterNode right = ParseTerm();
                    left = new ScimOrNode(left, right);
                    SkipWs();

                    continue;
                }

                break;
            }

            return left;
        }

        private ScimFilterNode ParseTerm()
        {
            SkipWs();

            if (!TryConsume('('))
                return ParseAttrExpression();
            ScimFilterNode inner = ParseFilter();

            return !TryConsume(')') ? throw new ScimFilterParseException("Expected ')'.") : inner;
        }

        private ScimFilterNode ParseAttrExpression()
        {
            string path = ReadAttributePath();
            SkipWs();

            if (PeekKeyword("pr"))
            {
                ConsumeKeyword("pr");

                return new ScimPresentNode(path);
            }

            string op = ReadCompareOp();
            SkipWs();
            string value = ReadCompareValue();

            return new ScimComparisonNode(path, op, value);
        }

        private string ReadAttributePath()
        {
            StringBuilder sb = new();
            AppendAttributePathSegments(sb);

            return sb.ToString();
        }

        private void AppendAttributePathSegments(StringBuilder sb)
        {
            sb.Append(ReadAttrSegment());
            SkipWs();
            AppendOptionalBracket(sb);

            while (true)
            {
                SkipWs();

                if (!TryConsume('.'))
                    break;

                sb.Append('.');
                sb.Append(ReadAttrSegment());
                SkipWs();
                AppendOptionalBracket(sb);
            }
        }

        private void AppendOptionalBracket(StringBuilder sb)
        {
            if (!TryConsume('['))
                return;

            ScimFilterNode inner = ParseFilter();
            SkipWs();

            if (!TryConsume(']'))
                throw new ScimFilterParseException($"Expected ']' after attribute selector filter at position {Position}.");

            sb.Append('[');
            sb.Append(CanonicalBracketInnerFilter(inner));
            sb.Append(']');
        }

        private static string CanonicalBracketInnerFilter(ScimFilterNode inner)
        {
            if (inner is not ScimComparisonNode c)
                throw new ScimFilterParseException(
                    "Attribute selectors must use a single comparison inside brackets (for example type eq \"work\").");

            string escaped = c.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

            return $"{c.AttributePath.ToLowerInvariant()} {c.Operator.ToLowerInvariant()} \"{escaped}\"";
        }

        private string ReadAttrSegment()
        {
            if (Eof || (!char.IsLetter(_s[Position]) && _s[Position] != '_'))
                throw new ScimFilterParseException($"Attribute path segment expected at {Position}.");

            int start = Position;

            while (!Eof)
            {
                char ch = _s[Position];

                if (char.IsLetterOrDigit(ch) || ch is '_' or '-')
                {
                    Position++;

                    continue;
                }

                break;
            }

            return _s[start..Position].ToString();
        }

        private string ReadCompareOp()
        {
            if (Position + 1 >= _s.Length)
                throw new ScimFilterParseException("Compare operator expected.");

            ReadOnlySpan<char> two = _s.Slice(Position, 2);
            string[] ops = ["eq", "ne", "co", "sw", "ew", "gt", "lt", "ge", "le"];

            foreach (string op in ops)
            {
                if (!two.Equals(op.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;
                Position += 2;

                return op.ToLowerInvariant();
            }

            throw new ScimFilterParseException($"Unknown compare operator at {Position}.");
        }

        private string ReadCompareValue()
        {
            SkipWs();

            if (Eof)
                throw new ScimFilterParseException("Compare value expected.");

            if (_s[Position] == '"')
                return ReadQuotedString();

            int start = Position;

            while (!Eof)
            {
                char ch = _s[Position];

                if (char.IsWhiteSpace(ch) || ch is ')')
                    break;

                Position++;
            }

            return _s[start..Position].ToString();
        }

        private string ReadQuotedString()
        {
            if (!TryConsume('"'))
                throw new ScimFilterParseException("Opening '\"' expected.");

            StringBuilder sb = new();

            while (!Eof)
            {
                char ch = _s[Position];

                if (ch == '\\')
                {
                    Position++;

                    if (Eof)
                        throw new ScimFilterParseException("Unterminated escape in string literal.");

                    sb.Append(_s[Position]);
                    Position++;

                    continue;
                }

                if (ch == '"')
                {
                    Position++;

                    return sb.ToString();
                }

                sb.Append(ch);
                Position++;
            }

            throw new ScimFilterParseException("Unterminated string literal.");
        }

        public void SkipWs()
        {
            while (!Eof && char.IsWhiteSpace(_s[Position]))
                Position++;
        }

        private bool TryConsume(char ch)
        {
            SkipWs();

            if (Eof || _s[Position] != ch)
                return false;

            Position++;

            return true;
        }

        private bool PeekKeyword(string word)
        {
            SkipWs();

            if (Position + word.Length > _s.Length)
                return false;

            return _s.Slice(Position, word.Length).Equals(word.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                   (Position + word.Length == _s.Length || !char.IsLetterOrDigit(_s[Position + word.Length]));
        }

        private void ConsumeKeyword(string word)
        {
            if (!PeekKeyword(word))
                throw new ScimFilterParseException($"Expected keyword '{word}' at {Position}.");

            SkipWs();
            Position += word.Length;
        }
    }
}
