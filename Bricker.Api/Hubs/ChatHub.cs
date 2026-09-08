using Bricker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Hubs;

[Authorize]
public sealed class ChatHub(BrickerDbContext db) : Hub
{
    public static string GroupName(Guid conversationId) => $"conversation:{conversationId:N}";
    public static string UserGroup(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Context.UserIdentifier;
        var allowed = await db.Conversations.AsNoTracking().AnyAsync(conversation =>
            conversation.Id == conversationId &&
            (conversation.BuyerId == userId || conversation.SellerId == userId));
        if (!allowed) throw new HubException("Conversa não encontrada.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }
}
