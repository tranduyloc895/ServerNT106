using Microsoft.AspNetCore.SignalR;

namespace API_Server.Hubs
{
    public class GroupChatHub: Hub
    {
        public async Task SendMessageGroup(string groupId, string username, string message)
        {
            await Clients.Group(groupId).SendAsync("ReceiveMessage",username,message);
        }
        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            await Clients.Group(groupId).SendAsync("UserJoined", Context.ConnectionId);
        }
        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
            await Clients.Group(groupId).SendAsync("UserLeft", Context.ConnectionId);
        }
    }
}
