using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;

namespace CardsOverLan.Web
{
	internal sealed class WebappBootstrapper : DefaultNancyBootstrapper
	{
		public string WebRoot { get; }

		public WebappBootstrapper(string webRoot)
		{
			WebRoot = webRoot;
			MimeTypes.AddType(".svg", "image/svg+xml");
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("packs", "./packs", ".jpg", ".png", ".svg"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", WebRoot));
			base.ConfigureConventions(nancyConventions);
		}

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
		{
			base.RequestStartup(container, pipelines, context);
			pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
			{
				ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
							.WithHeader("Access-Control-Allow-Methods", "POST,GET")
							.WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type")
							.WithHeader("Content-Security-Policy", @"default-src 'self'; script-src-attr 'self'; connect-src *; img-src 'self' data:; style-src 'self' 'unsafe-inline'");

			});
		}		
	}
}
