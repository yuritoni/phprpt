using System;
using System.Web.Http;

namespace CrystalReportsBridge.Controllers
{
    [RoutePrefix("api/v1/health")]
    public sealed class HealthController : ApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            return Ok(new { status = "ok", service = "crystal-reports-bridge", utc = DateTime.UtcNow });
        }
    }
}
