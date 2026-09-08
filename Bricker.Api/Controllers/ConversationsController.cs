using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Hubs;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController(BrickerDbContext db, UserManager<AppUser> userManager, IHubContext<ChatHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ConversationSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var rows = await db.Conversations.AsNoTracking()
            .Where(conversation => conversation.BuyerId == userId || conversation.SellerId == userId)
            .Select(conversation => new
            {
                conversation.Id,
                conversation.ListingId,
                ListingTitle = conversation.Listing.Title,
                ListingImageUrl = conversation.Listing.ImageUrl,
                ListingStatus = conversation.Listing.Status,
                OtherUserId = conversation.BuyerId == userId ? conversation.SellerId : conversation.BuyerId,
                OtherUserDisplayName = conversation.BuyerId == userId ? conversation.Seller.DisplayName : conversation.Buyer.DisplayName,
                LastMessage = conversation.Messages.OrderByDescending(message => message.CreatedAtUtc).Select(message => message.Body).FirstOrDefault(),
                conversation.LastMessageAtUtc,
                UnreadCount = conversation.Messages.Count(message => message.SenderId != userId && message.ReadAtUtc == null)
            })
            .OrderByDescending(conversation => conversation.LastMessageAtUtc)
            .ThenByDescending(conversation => conversation.Id)
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(row => new ConversationSummaryResponse(row.Id, row.ListingId, row.ListingTitle, row.ListingImageUrl,
            row.ListingStatus, row.OtherUserId, row.OtherUserDisplayName, row.LastMessage, row.LastMessageAtUtc, row.UnreadCount)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var count = await db.ChatMessages.AsNoTracking().CountAsync(message =>
            message.SenderId != userId && message.ReadAtUtc == null &&
            (message.Conversation.BuyerId == userId || message.Conversation.SellerId == userId), cancellationToken);
        return Ok(new { count });
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyCollection<ChatMessageResponse>>> Messages(Guid id, [FromQuery] DateTime? before, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var userId = userManager.GetUserId(User)!;
        if (!await IsParticipant(id, userId, cancellationToken)) return NotFound();
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ChatMessages.AsNoTracking().Where(message => message.ConversationId == id);
        if (before is not null) query = query.Where(message => message.CreatedAtUtc < before);
        var messages = await query.OrderByDescending(message => message.CreatedAtUtc).Take(pageSize)
            .Select(message => new ChatMessageResponse(message.Id, message.ConversationId, message.SenderId,
                message.Sender.DisplayName, message.Body, message.CreatedAtUtc, message.ReadAtUtc))
            .ToListAsync(cancellationToken);
        messages.Reverse();
        return Ok(messages);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<ChatMessageResponse>> Send(Guid id, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var conversation = await db.Conversations.SingleOrDefaultAsync(item => item.Id == id && (item.BuyerId == userId || item.SellerId == userId), cancellationToken);
        if (conversation is null) return NotFound();
        var body = request.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body) || body.Length > 2_000) return BadRequest(new { message = "A mensagem deve ter entre 1 e 2.000 caracteres." });
        var sender = await userManager.GetUserAsync(User);
        if (sender is null) return Unauthorized();
        var message = new ChatMessage { ConversationId = id, SenderId = userId, Body = body };
        conversation.LastMessageAtUtc = message.CreatedAtUtc;
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        var response = new ChatMessageResponse(message.Id, id, userId, sender.DisplayName, message.Body, message.CreatedAtUtc, null);
        await hub.Clients.Group(ChatHub.GroupName(id)).SendAsync("MessageReceived", response, cancellationToken);
        await hub.Clients.Groups(ChatHub.UserGroup(conversation.BuyerId), ChatHub.UserGroup(conversation.SellerId))
            .SendAsync("ConversationUpdated", id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        if (!await IsParticipant(id, userId, cancellationToken)) return NotFound();
        var readAt = DateTime.UtcNow;
        await db.ChatMessages.Where(message => message.ConversationId == id && message.SenderId != userId && message.ReadAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(message => message.ReadAtUtc, readAt), cancellationToken);
        await hub.Clients.Group(ChatHub.GroupName(id)).SendAsync("MessagesRead", new { conversationId = id, readerId = userId, readAtUtc = readAt }, cancellationToken);
        await hub.Clients.Group(ChatHub.UserGroup(userId)).SendAsync("ConversationUpdated", id, cancellationToken);
        return NoContent();
    }

    private Task<bool> IsParticipant(Guid conversationId, string userId, CancellationToken cancellationToken) =>
        db.Conversations.AsNoTracking().AnyAsync(conversation => conversation.Id == conversationId &&
            (conversation.BuyerId == userId || conversation.SellerId == userId), cancellationToken);
}
