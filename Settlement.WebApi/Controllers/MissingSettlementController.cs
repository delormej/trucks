using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Settlement.WebApi.BackgroundServices;

namespace Settlement.WebApi.Controllers
{
    [ApiController]
    [Route("missing")]
    public class MissingSettlementController : ControllerBase
    {
        private readonly ILogger<MissingSettlementController> _logger;
        private readonly MissingSettlementService _service;

        public MissingSettlementController(ILogger<MissingSettlementController> logger, [FromServices] MissingSettlementService service)
        {
            _logger = logger;
            _service = service;
        }

        /// <Summary>
        /// Dummy method which recieves 2 parameters.
        /// </Summary>
        /// <Example>
        /// To call this method, use the following EXACT syntax:
        ///     curl -i -X POST -H "Content-Type: application/json" -H "Accept: application/json" -d '{"CompanyId": "3333", "Password": "bar"}' http://localhost:5000/missing
        /// </Example>
        [HttpPost]
        public IEnumerable<string> Post([FromBody]PantherCredentials panther)
        {
            _logger.LogInformation($"Calling foo... {panther.CompanyId}, {panther.Password}");
            _service.FooAsync();
            string[] empty = new string[] {"1", "2"};
            return empty;
        }
    }

    public class PantherCredentials
    {
        public string CompanyId { get; set; }
        public string Password { get; set; }
    }
}
