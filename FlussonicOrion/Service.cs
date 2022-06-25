using FlussonicOrion.Managers;
using FlussonicOrion.OrionPro;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlussonicOrion
{
    public class Service : BackgroundService
    {
        private readonly ILogger<Service> _logger;
        private IOrionClient _orionClient;
        private IServiceSettingsController _serviceSettingsController;
        private IVideoSourceManager _videoSourceManager;
        private IAccessPointsManager _accessPointsManager;
        private EntryPointsHelper _entryPointsHelper;
        private HttpServer _httpServer;
        private FlussonicServer _flussonicServer;

        public Service(ILogger<Service> logger, 
                       IOrionClient orionClient,
                       IServiceSettingsController serviceSettingsController,
                       IVideoSourceManager videoSourceManager,
                       IAccessPointsManager accessPointsManager,
                       HttpServer httpServer,
                       EntryPointsHelper entryPointsHelper,
                       FlussonicServer flussonicServer)
        {
            _logger = logger;
            _orionClient = orionClient;
            _serviceSettingsController = serviceSettingsController;
            _videoSourceManager = videoSourceManager;
            _accessPointsManager = accessPointsManager;
            _httpServer = httpServer;
            _entryPointsHelper = entryPointsHelper;
            _flussonicServer = flussonicServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Запуск службы");
                _serviceSettingsController.Initialize();
                await _orionClient.Initialize(_serviceSettingsController.Settings.OrionSettings);
                _videoSourceManager.Initialize(_serviceSettingsController.Settings.AccesspointsSettings);
                _accessPointsManager.Initialize();
                _entryPointsHelper.Initialize();
                _flussonicServer.Start();
                _httpServer.Start();
                _logger.LogInformation("Служба запущена");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации службы");
            }
        }

        public override void Dispose()
        {
            _flussonicServer?.Stop();
            _orionClient?.Dispose();
            _httpServer?.Stop();
            base.Dispose();
        }
    }
}
