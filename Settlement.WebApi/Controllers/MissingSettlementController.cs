using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Settlement.WebApi.Controllers
{
    [ApiController]
    [Route("missing")]
    public class MissingSettlementController : ControllerBase
    {
        private readonly ILogger<MissingSettlementController> _logger;

        public MissingSettlementController(ILogger<MissingSettlementController> logger)
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
