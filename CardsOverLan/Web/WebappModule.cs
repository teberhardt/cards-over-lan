using CardsOverLan.Analytics;
using Nancy;
using Newtonsoft.Json;

namespace CardsOverLan.Web
{
	public class WebappModule : NancyModule
	{
		public WebappModule() : base("")
		{
			Get["/gameinfo"] = p => Response.AsText(JsonConvert.SerializeObject(GameManager.Instance.GetGameInfoObject(), Formatting.None), "application/json");
			Get["/"] = p =>
			{
				if (!string.IsNullOrWhiteSpace(Request.Headers.Referrer))
				{
					AnalyticsManager.Instance.RecordReferer(Request.Headers.Referrer.Trim());
				}
				return Response.AsFile($"{GameManager.Instance.Settings.WebRoot}/index.html", "text/html");
			};
		}
	}
}
