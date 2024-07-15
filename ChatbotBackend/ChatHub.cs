using Microsoft.AspNetCore.SignalR;

namespace ChatbotBackend.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task TypingNotification(string user)
        {
            await Clients.Others.SendAsync("UserTyping", user);
        }

        public async Task StopTypingNotification(string user)
        {
            await Clients.Others.SendAsync("UserStopTyping", user);
        }
    }
}
