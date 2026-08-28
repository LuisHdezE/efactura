using ApplicationCore.Constants;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public InfoController(IConfiguration _configuration)
        {
            configuration = _configuration;
        }

        /// <summary>
        ///  obtener ping de la api
        /// </summary>
        /// <returns></returns>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(Messages.PingMessage);
        }

        /// <summary>
        /// retorna la version de la aplicacion
        /// </summary>
        /// <returns></returns>
        [HttpGet("version")]
        public IActionResult Info()
        {
            var appInfo = new
            {
                AppName = configuration["App:Name"],
                AppVersion = configuration["App:Version"]
            };
            return Ok(appInfo);
        }

        /// <summary>
        /// retorna la fecha y hora actual
        /// </summary>
        /// <returns></returns>
        [HttpGet("fecha")]
        public IActionResult getFecha()
        {
            return Ok(DateTime.Now);
        }
    }
}