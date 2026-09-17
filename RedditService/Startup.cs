using Owin;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(RedditService.Startup))]

namespace RedditService
{
    /// <summary>
    /// OWIN startup klasa – automatski je detektuje Microsoft.Owin.Host.SystemWeb
    /// pri pokretanju aplikacije (preko [assembly: OwinStartup]).
    /// Ovde mapiramo SignalR hub rute (endpoint "/signalr" + "/signalr/hubs" proxy).
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
