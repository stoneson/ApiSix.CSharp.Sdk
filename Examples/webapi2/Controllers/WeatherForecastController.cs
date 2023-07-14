using Microsoft.AspNetCore.Mvc;

namespace webapi2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/WeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        /// <summary>
        /// ½¡¿µ¼ì²é
        /// </summary>
        /// <returns></returns>
        [HttpGet("/Health")]
        public string Health()
        {
            var Server = HttpContext.Request.Headers["Host"];
            ApiSix.CSharp.XTrace.WriteLine(Server + " Ok2 @ " + DateTime.Now.ToString("yyyyy-MM-dd HH:mm:ss.fff"));
            return "Ok2 @ "+DateTime.Now.ToString("yyyyy-MM-dd HH:mm:ss.fff");
        }
    }
}