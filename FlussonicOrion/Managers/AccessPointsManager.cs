using FlussonicOrion.Controllers;
using FlussonicOrion.Filters;
using FlussonicOrion.OrionPro;
using FlussonicOrion.OrionPro.DataSources;
using FlussonicOrion.OrionPro.Models;
using FlussonicOrion.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private IAccessController _accessController;
        private IServiceScopeFactory _scopeFactory;
        private List<AccessPointController> _accessPoints;
        private IServiceSettingsController _serviceSettingsController;
        private IOrionClient _orionClient;

        public AccessPointsManager(ILogger<AccessPointsManager> logger, 
                                   IServiceScopeFactory scopeFactory, 
                                   IServiceSettingsController serviceSettingsController,
                                   IOrionClient orionClient)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceSettingsController = serviceSettingsController;
            _orionClient = orionClient;
            _accessPoints = new List<AccessPointController>();
        }

        public void Initialize()
        {
            try
            {
                var orionSettings = _serviceSettingsController.Settings.OrionSettings;
                _dataSource = CreateDataSource(orionSettings);
                _dataSource.Initialize();
                _accessController = new AccessController(_dataSource);

                var accessPoints = _serviceSettingsController
                    .Settings
                    .AccesspointsSettings
                    .Select(x =>
                        new AccessPointController(
                            x.AccesspointId, 
                            CreateFilter(x.AccesspointId, x.FilterType), 
                            _logger, 
                            _accessController, 
                            _orionClient));
                _accessPoints.AddRange(accessPoints);

                _logger.LogInformation("AccessPointManager инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации AccessPointManager");
            }
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
    }
}
