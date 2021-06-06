using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IController _controller;

        public Worker(ILogger<Worker> logger, IController controller)
        {
            _logger = logger;
            _controller = controller;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service starting");
            await _controller.Initialize();
            _logger.LogInformation("Service started");
        }

        public override void Dispose()
        {
            _controller.Dispose();
            base.Dispose();
        }
    }
}
