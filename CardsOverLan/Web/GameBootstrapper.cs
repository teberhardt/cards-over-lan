using Nancy;
using Nancy.Conventions;

namespace CardsOverLan.Web
{
    internal sealed class GameBootstrapper : DefaultNancyBootstrapper
    {
        public GameBootstrapper()
        {
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "./web_content"));
            base.ConfigureConventions(nancyConventions);
        }

        //protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();

        private sealed class CustomRootPathProvider : IRootPathProvider
        {
            public string GetRootPath()
            {
                return "./web_content";
            }
        }
    }
}
