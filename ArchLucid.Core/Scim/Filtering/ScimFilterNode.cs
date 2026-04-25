namespace ArchLucid.Core.Scim.Filtering;

public abstract record ScimFilterNode;

public sealed record ScimComparisonNode(string AttributePath, string Operator, string Value) : ScimFilterNode;

public sealed record ScimPresentNode(string AttributePath) : ScimFilterNode;

public sealed record ScimNotNode(ScimFilterNode Inner) : ScimFilterNode;

public sealed record ScimAndNode(ScimFilterNode Left, ScimFilterNode Right) : ScimFilterNode;

public sealed record ScimOrNode(ScimFilterNode Left, ScimFilterNode Right) : ScimFilterNode;
