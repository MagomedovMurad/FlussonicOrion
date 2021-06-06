using FlussonnicOrion.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ILogicController _controller;

        public Worker(ILogger<Worker> logger, ILogicController controller)
        {
            _logger = logger;
            _controller = controller;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запуск службы");
            await _controller.Initialize();
            _logger.LogInformation("Служба запущена");
        }

        public override void Dispose()
        {
            _controller.Dispose();
            base.Dispose();
        }
    }
}
