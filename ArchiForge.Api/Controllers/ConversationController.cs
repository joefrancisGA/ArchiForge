using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Conversation;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Provides read access to conversation threads and their messages.
/// All results are scoped to the caller's tenant, workspace, and project.
/// </summary>
/// <remarks>Routes under <c>api/conversations</c>.</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/conversations")]
[EnableRateLimiting("fixed")]
public sealed class ConversationController(
    IConversationThreadRepository threadRepository,
    IConversationMessageRepository messageRepository,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Lists conversation threads for the current scope.</summary>
    /// <param name="take">Number of threads to return (clamped to 1–200; defaults to 50).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListThreads(int take = 50, CancellationToken ct = default)
    {
        var safeTake = Math.Clamp(take, 1, 200);
        var scope = scopeProvider.GetCurrentScope();

        var threads = await threadRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            safeTake,
            ct);

        return Ok(threads);
    }

    /// <summary>Returns messages for the specified conversation thread.</summary>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="take">Number of messages to return (clamped to 1–500; defaults to 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Message list, or 404 when the thread does not exist or belongs to another scope.</returns>
    [HttpGet("{threadId:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid threadId, int take = 100, CancellationToken ct = default)
    {
        var safeTake = Math.Clamp(take, 1, 500);
        var scope = scopeProvider.GetCurrentScope();
        var thread = await threadRepository.GetByIdAsync(threadId, ct);
        if (thread is null ||
            thread.TenantId != scope.TenantId ||
            thread.WorkspaceId != scope.WorkspaceId ||
            thread.ProjectId != scope.ProjectId)
        {
            return this.NotFoundProblem(
                $"Conversation thread '{threadId}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);
        }

        var messages = await messageRepository.GetByThreadIdAsync(threadId, safeTake, ct);
        return Ok(messages);
    }
}
