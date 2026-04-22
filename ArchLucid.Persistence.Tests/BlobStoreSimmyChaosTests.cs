using ArchLucid.Persistence.BlobStore;

using FluentAssertions;

using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// Simmy fault injection on a synthetic <see cref="IArtifactBlobStore"/> write path with Polly retry (mirrors local/blob IO failures).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class BlobStoreSimmyChaosTests
{
    private sealed class MemoryBlobStore : IArtifactBlobStore
    {
        private readonly Dictionary<string, string> _blobs = new(StringComparer.Ordinal);

        public Task<string> WriteAsync(string containerName, string blobName, string content, CancellationToken ct)
        {
            string key = $"{containerName}/{blobName}";
            _blobs[key] = content;

            return Task.FromResult($"mem:{key}");
        }

        public Task<string?> ReadAsync(string blobUri, CancellationToken ct)
        {
            if (!blobUri.StartsWith("mem:", StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(null);
            }

            string key = blobUri[4..];

            return Task.FromResult(_blobs.TryGetValue(key, out string? v) ? v : null);
        }
    }

    [Fact]
    public async Task Chaos_io_faults_on_writes_retry_then_persist()
    {
        MemoryBlobStore inner = new();
        int chaosWave = 0;

        ChaosFaultStrategyOptions chaosOptions = new()
        {
            InjectionRate = 1.0,
            EnabledGenerator = _ => new ValueTask<bool>(Interlocked.Increment(ref chaosWave) <= 2),
            FaultGenerator = _ => new ValueTask<Exception?>(new IOException("simulated blob store fault"))
        };

        ResiliencePipeline<string> pipeline = new ResiliencePipelineBuilder<string>()
            .AddRetry(
                new RetryStrategyOptions<string>
                {
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromMilliseconds(1),
                    ShouldHandle = new PredicateBuilder<string>().Handle<IOException>(),
                })
            .AddChaosFault(chaosOptions)
            .Build();

        string uri = await pipeline.ExecuteAsync(
            async ct => await inner.WriteAsync("artifacts", "payload.json", "{}", ct),
            CancellationToken.None);

        uri.Should().StartWith("mem:artifacts/payload.json");
        chaosWave.Should().Be(3);

        string? body = await inner.ReadAsync(uri, CancellationToken.None);

        body.Should().Be("{}");
    }
}
