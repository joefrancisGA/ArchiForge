using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Conversation;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/conversations")]
[EnableRateLimiting("fixed")]
public sealed class ConversationController : ControllerBase
{
    private readonly IConversationThreadRepository _threadRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IScopeContextProvider _scopeProvider;

    public ConversationController(
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository,
        IScopeContextProvider scopeProvider)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _scopeProvider = scopeProvider;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListThreads(int take = 50, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        var threads = await _threadRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            take,
            ct);

        return Ok(threads);
    }

    [HttpGet("{threadId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid threadId, int take = 100, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var thread = await _threadRepository.GetByIdAsync(threadId, ct);
        if (thread is null ||
            thread.TenantId != scope.TenantId ||
            thread.WorkspaceId != scope.WorkspaceId ||
            thread.ProjectId != scope.ProjectId)
        {
            return NotFound();
        }

        var messages = await _messageRepository.GetByThreadIdAsync(threadId, take, ct);
        return Ok(messages);
    }
}
