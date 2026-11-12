using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CrystalReportsBridge.Infrastructure
{
    public sealed class ApiKeyAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var expected = ConfigurationManager.AppSettings["ApiKey"] ?? string.Empty;
            var supplied = actionContext.Request.Headers.Contains("X-API-Key")
                ? actionContext.Request.Headers.GetValues("X-API-Key").FirstOrDefault() ?? string.Empty
                : string.Empty;

            if (expected.Length < 32 || !FixedTimeEquals(expected, supplied))
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "unauthorized", message = "A valid X-API-Key header is required." });
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left);
            var b = Encoding.UTF8.GetBytes(right);
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
