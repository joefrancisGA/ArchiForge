using ArchLucid.Application.Identity;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Identity;

public sealed class TrialLocalIdentityServiceTests
{
    private static TrialAuthOptions CreateTrialOptions()
    {
        return new TrialAuthOptions
        {
            Modes = [TrialAuthModeConstants.LocalIdentity],
            LocalIdentity = new TrialLocalIdentityOptions { MinimumPasswordLength = 8, MaximumPasswordLength = 128, PwnedPasswordRangeCheckEnabled = false }
        };
    }

    private static PwnedPasswordRangeClient CreatePwnedClient(TrialAuthOptions options)
    {
        HttpClient http = new();
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        return new PwnedPasswordRangeClient(http, cache, Options.Create(options));
    }

    private static TrialLocalIdentityService CreateSut(
        Mock<ITrialIdentityUserRepository> repository,
        Mock<ITrialLocalIdentityAccountExistsNotifier> accountExistsNotifier,
        TrialAuthOptions? options = null)
    {
        TrialAuthOptions opts = options ?? CreateTrialOptions();

        return new TrialLocalIdentityService(
            Options.Create(opts),
            repository.Object,
            new PasswordHasher<TrialIdentityHasherUser>(),
            new TrialPasswordPolicyValidator(Options.Create(opts)),
            CreatePwnedClient(opts),
            accountExistsNotifier.Object,
            NullLogger<TrialLocalIdentityService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_when_email_already_exists_sends_notice_and_does_not_create_user()
    {
        TrialAuthOptions opts = CreateTrialOptions();
        Mock<ITrialIdentityUserRepository> repository = new();
        Mock<ITrialLocalIdentityAccountExistsNotifier> notifier = new();

        string normalized = TrialEmailNormalizer.Normalize("dup@example.com");

        repository
            .Setup(r => r.GetByNormalizedEmailAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrialIdentityUserRecord { Id = Guid.NewGuid(), NormalizedEmail = normalized });

        TaskCompletionSource notified = new(TaskCreationOptions.RunContinuationsAsynchronously);

        notifier
            .Setup(n => n.NotifyAccountAlreadyExistsAsync("dup@example.com", CancellationToken.None))
            .Callback(() => notified.TrySetResult())
            .Returns(Task.CompletedTask);

        TrialLocalIdentityService sut = CreateSut(repository, notifier, opts);

        TrialLocalRegistrationResult result = await sut.RegisterAsync(
            "dup@example.com",
            "long-enough-secret",
            CancellationToken.None);

        await notified.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.False(string.IsNullOrEmpty(result.VerificationToken));

        repository.Verify(
            r => r.CreatePendingUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        notifier.Verify(
            n => n.NotifyAccountAlreadyExistsAsync("dup@example.com", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_when_email_is_new_creates_user_and_does_not_notify_duplicate()
    {
        TrialAuthOptions opts = CreateTrialOptions();
        Mock<ITrialIdentityUserRepository> repository = new();
        Mock<ITrialLocalIdentityAccountExistsNotifier> notifier = new();

        string normalized = TrialEmailNormalizer.Normalize("new@example.com");

        repository
            .Setup(r => r.GetByNormalizedEmailAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrialIdentityUserRecord?)null);

        repository
            .Setup(r => r.CreatePendingUserAsync(
                normalized,
                "new@example.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        TrialLocalIdentityService sut = CreateSut(repository, notifier, opts);

        await sut.RegisterAsync("new@example.com", "long-enough-secret", CancellationToken.None);

        notifier.Verify(
            n => n.NotifyAccountAlreadyExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_when_email_unknown_returns_null()
    {
        TrialAuthOptions opts = CreateTrialOptions();
        Mock<ITrialIdentityUserRepository> repository = new();
        Mock<ITrialLocalIdentityAccountExistsNotifier> notifier = new();

        string normalized = TrialEmailNormalizer.Normalize("ghost@example.com");

        repository
            .Setup(r => r.GetByNormalizedEmailAsync(normalized, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrialIdentityUserRecord?)null);

        TrialLocalIdentityService sut = CreateSut(repository, notifier, opts);

        TrialLocalAuthResult? auth = await sut.AuthenticateAsync(
            "ghost@example.com",
            "long-enough-secret",
            CancellationToken.None);

        Assert.Null(auth);
    }
}
