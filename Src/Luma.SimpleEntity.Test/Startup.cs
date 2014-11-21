using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Luma.SimpleEntity.Test.Startup))]
namespace Luma.SimpleEntity.Test
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
