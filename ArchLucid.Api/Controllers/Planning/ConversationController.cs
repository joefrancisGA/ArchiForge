using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Conversation;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Conversation;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Planning;

/// <summary>
///     Provides read access to conversation threads and their messages.
///     All results are scoped to the caller's tenant, workspace, and project.
/// </summary>
/// <remarks>Routes under <c>api/conversations</c>.</remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/conversations")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class ConversationController(
    IConversationThreadRepository threadRepository,
    IConversationMessageRepository messageRepository,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Lists conversation threads for the current scope.</summary>
    /// <param name="take">
    ///     Number of threads to return (clamped to 1..<see cref="PaginationDefaults.MaxPageSize" />; defaults
    ///     to 50). Used when <paramref name="page" /> is not set.
    /// </param>
    /// <param name="page">One-based page number. When provided, the response is a <see cref="PagedResponse{T}" />.</param>
    /// <param name="pageSize">
    ///     Items per page (clamped 1..<see cref="PaginationDefaults.MaxPageSize" />; default 50). Only used
    ///     when <paramref name="page" /> is set.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationThread>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResponse<ConversationThread>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListThreads(
        int take = 50,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        if (page.HasValue)
        {
            (int safePage, int safePageSize) = PaginationDefaults.Normalize(page.Value, pageSize);
            int skip = PaginationDefaults.ToSkip(safePage, safePageSize);
            (IReadOnlyList<ConversationThread> items, int total) = await threadRepository.ListByScopePagedAsync(
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                skip,
                safePageSize,
                ct);

            return Ok(PagedResponseBuilder.FromDatabasePage(items, total, safePage, safePageSize));
        }

        int safeTake = Math.Clamp(take, 1, PaginationDefaults.MaxPageSize);

        IReadOnlyList<ConversationThread> threads = await threadRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            safeTake,
            ct);

        return Ok(threads);
    }

    /// <summary>Returns messages for the specified conversation thread.</summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="take">
    ///     Number of messages to return (clamped to 1..<see cref="PaginationDefaults.MaxListingTake" />;
    ///     defaults to 100).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Message list, or 404 when the thread does not exist or belongs to another scope.</returns>
    [HttpGet("{threadId:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid threadId, int take = 100, CancellationToken ct = default)
    {
        int safeTake = Math.Clamp(take, 1, PaginationDefaults.MaxListingTake);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        ConversationThread? thread = await threadRepository.GetByIdAsync(threadId, ct);

        if (thread is null ||
            thread.TenantId != scope.TenantId ||
            thread.WorkspaceId != scope.WorkspaceId ||
            thread.ProjectId != scope.ProjectId)

            return this.NotFoundProblem(
                $"Conversation thread '{threadId}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);


        IReadOnlyList<ConversationMessage>
            messages = await messageRepository.GetByThreadIdAsync(threadId, safeTake, ct);
        return Ok(messages);
    }
}
