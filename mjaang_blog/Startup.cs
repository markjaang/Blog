using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(mjaang_blog.Startup))]
namespace mjaang_blog
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
