using FlussonicOrion.Controllers;
using FlussonicOrion.Filters;
using FlussonicOrion.Flussonic;
using FlussonicOrion.Flussonic.Enums;
using FlussonicOrion.Models;
using FlussonicOrion.OrionPro;
using FlussonicOrion.OrionPro.DataSources;
using FlussonicOrion.OrionPro.Models;
using FlussonicOrion.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonicOrion.Managers
{
    public interface IAccessPointsManager
    {
        void Initialize();
        AccessPointController GetAcceessPoint(int id);
    }

    public class AccessPointsManager: IAccessPointsManager
    {
        private ILogger _logger;
        private IOrionDataSource _dataSource;
        private AccessChecker _accessChecker;
        private IServiceScopeFactory _scopeFactory;
        private List<AccessPointController> _accessPoints;
        private IServiceSettingsController _serviceSettingsController;
        private IOrionClient _orionClient;
        private FlussonicServer _flussonicServer;
        private IVideoSourceManager _videoSourceManager;

        public AccessPointsManager(ILogger<AccessPointsManager> logger, 
                                   IServiceScopeFactory scopeFactory, 
                                   IServiceSettingsController serviceSettingsController,
                                   IOrionClient orionClient,
                                   FlussonicServer flussonicServer,
                                   IVideoSourceManager videoSourceManager)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceSettingsController = serviceSettingsController;
            _orionClient = orionClient;
            _flussonicServer = flussonicServer;
            _videoSourceManager = videoSourceManager;
            _accessPoints = new List<AccessPointController>();
        }

        public void Initialize()
        {
            try
            {
                var orionSettings = _serviceSettingsController.Settings.OrionSettings;
                _dataSource = CreateDataSource(orionSettings);
                _dataSource.Initialize();
                _accessChecker = new AccessChecker(_dataSource);
                _accessPoints.AddRange(GetAccessPoints());
                _flussonicServer.NewEvent += Flussonic_NewEvent;
                _logger.LogInformation("AccessPointManager инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации AccessPointManager");
            }
        }

        private void Flussonic_NewEvent(object sender, FlussonicEvent e)
        {
            Task.Run(() =>
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
                    var accessPointController = GetAcceessPoint(videoSource.AccessPointId);

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

        public AccessPointController GetAcceessPoint(int id)
        {
            return _accessPoints.FirstOrDefault(x => x.Id == id);
        }
        private IOrionDataSource CreateDataSource(OrionSettings orionSettings)
        {
            var logger = _scopeFactory.Resolve<ILogger<IOrionDataSource>>();

            if (orionSettings.UseCache)
                return new OrionCacheDataSource(orionSettings.CacheUpdatingInterval, _orionClient, logger);
            else
                return new OrionClientDataSource(_orionClient, logger);
        }
        private IFilter CreateFilter(int accessPointId, FilterType filterType)
        {
            switch (filterType)
            {
                case FilterType.Empty:
                    return new EmptyFilter();
                case FilterType.Crosscam:
                    return new CrossCamerasFilter(accessPointId, _logger, _orionClient, _serviceSettingsController);
                case FilterType.Opentimeout:
                    return new OpenTimeoutFilter(accessPointId, _logger, _orionClient, _serviceSettingsController);
                default: 
                    throw new InvalidCastException($"Тип фильтра {filterType} не поддерживается");
            }
        }
        private IEnumerable<AccessPointController> GetAccessPoints()
        {
            return _serviceSettingsController
                        .Settings
                        .AccesspointsSettings
                        .Select(x =>
                            new AccessPointController(
                                x.AccesspointId,
                                CreateFilter(x.AccesspointId, x.FilterType),
                                _logger,
                                _accessChecker,
                                _orionClient));
        }
    }
}
