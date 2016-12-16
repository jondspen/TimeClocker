using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TimeClocker.Startup))]
namespace TimeClocker
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
