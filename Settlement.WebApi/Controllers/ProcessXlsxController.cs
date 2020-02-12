using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Settlement.WebApi.Controllers
{
    [ApiController]
    [Route("process")]
    public class ProcessXlsxController : ControllerBase
    {
        private readonly ILogger<ProcessXlsxController> _logger;

        public ProcessXlsxController(ILogger<ProcessXlsxController> logger)
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
