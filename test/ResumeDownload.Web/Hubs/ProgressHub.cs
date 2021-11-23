using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ResumeDownload.Web.Hubs
{
    public class ProgressHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
