using FlussonicOrion.Flussonic;
using FlussonicOrion.Flussonic.Enums;
using FlussonicOrion.Managers;
using FlussonicOrion.Models;
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

        private IFlussonic _flussonic;
        

        public Service(ILogger<Service> logger, 
                       IOrionClient orionClient,
                       IServiceSettingsController serviceSettingsController,
                       IVideoSourceManager videoSourceManager,
                       IAccessPointsManager accessPointsManager)
        {
            _logger = logger;
            _orionClient = orionClient;
            _serviceSettingsController = serviceSettingsController;
            _videoSourceManager = videoSourceManager;
            _accessPointsManager = accessPointsManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запуск службы");
            await Initialize();
            _logger.LogInformation("Служба запущена");
        }

        public override void Dispose()
        {
            _flussonic?.Stop();
            _orionClient?.Dispose();
            base.Dispose();
        }

        private async Task Initialize()
        {
            try
            {
                _serviceSettingsController.Initialize();
                await _orionClient.Initialize(_serviceSettingsController.Settings.OrionSettings);

                _videoSourceManager.Initialize(_serviceSettingsController.Settings.AccesspointsSettings);
                _accessPointsManager.Initialize();

                var httpServer = new HttpServer(_serviceSettingsController.Settings.ServerSettings.ServerPort);
                httpServer.Start();

                _entryPointsHelper = new EntryPointsHelper(httpServer, _orionClient, _logger);
                _entryPointsHelper.Initialize();

                _flussonic = new FlussonicServer(httpServer, _logger);
                _flussonic.Start();
                _flussonic.NewEvent += Flussonic_NewEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации службы");
            }
        }

        private void Flussonic_NewEvent(object sender, FlussonicEvent e)
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"Новое событие от камеры: {e.CameraId}. Гос. номер: {e.ObjectId}. Action: {e.ObjectAction}");
                    if (e.ObjectClass != ObjectClass.Vehicle)
                    {
                        _logger.LogWarning($"Объект типа {e.ObjectClass} не поддерживается");
                        return;
                    }

                    var videoSource = _videoSourceManager.GetVideoSource(e.CameraId);
                    if (videoSource == null)
                    {
                        _logger.LogWarning($"Камера с идентификатором {e.CameraId} не привязана к точке доступа");
                        return;
                    }
                    var accessPointController = _accessPointsManager.GetAcceessPoint(videoSource.AccessPointId);

                    if (e.ObjectAction == ObjectAction.Enter)
                        accessPointController.OnEnter(e.ObjectId, videoSource.PassageDirection);
                    else if (e.ObjectAction == ObjectAction.Leave)
                        accessPointController.OnLeave(e.ObjectId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при обработке номера {e.ObjectId}");
                }
            });
        }
    }
}
