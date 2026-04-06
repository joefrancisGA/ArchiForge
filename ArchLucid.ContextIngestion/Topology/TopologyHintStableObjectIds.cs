using System.Security.Cryptography;
using System.Text;

namespace ArchiForge.ContextIngestion.Topology;

/// <summary>
/// Deterministic <see cref="Models.CanonicalObject.ObjectId"/> for topology hints so
/// cross-connector references (e.g. policy <c>applicableTopologyNodeIds</c>, <c>parentNodeId</c>)
/// align with <c>obj-{ObjectId}</c> graph node ids after ingestion.
/// </summary>
public static class TopologyHintStableObjectIds
{
    /// <summary>32 lowercase hex characters (128 bits of SHA-256).</summary>
    public static string FromHintName(string topologyHintName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topologyHintName);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(topologyHintName.Trim()));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }
}
