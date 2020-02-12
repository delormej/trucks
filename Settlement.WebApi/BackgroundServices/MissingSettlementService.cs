using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Settlement.WebApi.BackgroundServices
{
    public class MissingSettlementService : IHostedService
    {
        private readonly ILogger<MissingSettlementService> _logger;
        private Timer _timer;
        private int executionCount = 0;

        public MissingSettlementService(ILogger<MissingSettlementService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public void Foo()
        {
            _logger.LogInformation("Foo called!");
        }

        public void FooAsync()
        {
            var t = new Timer((o) => {
                _logger.LogInformation("Hello");
            }, null, 5000, -1);
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);
        }        
    }
}
