using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace RedditService.Hubs
{
    public class ChatHub : Hub
    {
        private string UserEmail
        {
            get
            {
                var email = Context.QueryString["userEmail"];
                return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
            }
        }

        public override Task OnConnected()
        {
            string email = UserEmail;
            if (email != null)
                Groups.Add(Context.ConnectionId, email);
            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            string email = UserEmail;
            if (email != null)
                Groups.Add(Context.ConnectionId, email);
            return base.OnReconnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string email = UserEmail;
            if (email != null)
                Groups.Remove(Context.ConnectionId, email);
            return base.OnDisconnected(stopCalled);
        }
    }
}
