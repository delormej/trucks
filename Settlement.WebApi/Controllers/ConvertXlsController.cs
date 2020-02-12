using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Settlement.WebApi.BackgroundServices;

namespace Settlement.WebApi.Controllers
{
    [ApiController]
    [Route("convertxls")]
    public class ConvertXlsController : ControllerBase
    {
        private readonly ILogger<ConvertXlsController> _logger;

        public ConvertXlsController(ILogger<ConvertXlsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {

            string[] empty = new string[] {"1", "2"};
            return empty;
        }
    }
}
