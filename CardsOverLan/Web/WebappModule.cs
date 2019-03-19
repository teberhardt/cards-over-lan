using Nancy;
using Newtonsoft.Json;

namespace CardsOverLan.Web
{
	public class WebappModule : NancyModule
	{
		public WebappModule() : base("")
		{
			Get["/gameinfo"] = p => Response.AsText(JsonConvert.SerializeObject(GameManager.Instance.GetGameInfoObject(), Formatting.None), "application/json");
			Get["/"] = p => Response.AsFile($"./web_content/index.html", "text/html");
		}
	}
}
