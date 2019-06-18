using System.IO;
using System.Text;
using Nancy;
using Nancy.ErrorHandling;

namespace CardsOverLan.Web
{
    public sealed class NotFoundStatusHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.NotFound;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            context.Response.Contents = stream =>
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(File.ReadAllText($"{GameManager.Instance.Settings.WebRoot}/404.html"));
                }
            };
            context.Response.WithStatusCode(HttpStatusCode.NotFound);
        }
    }
}