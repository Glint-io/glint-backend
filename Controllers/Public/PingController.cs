using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace glint_backend.Controllers.Public
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        private static readonly DateTime _startTime = DateTime.UtcNow;
        private readonly IWebHostEnvironment _env;

        public PingController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ping()
        {
            var uptime = DateTime.UtcNow - _startTime;

            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow,
                uptime = $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                environment = _env.EnvironmentName
            });
        }
    }
}