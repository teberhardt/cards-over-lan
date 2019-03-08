using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;

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

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
		{
			base.RequestStartup(container, pipelines, context);
			pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
			{
				ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
								.WithHeader("Access-Control-Allow-Methods", "POST,GET")
								.WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");

			});
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
