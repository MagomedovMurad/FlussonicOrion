using FlussonnicOrion.Controllers;
using FlussonnicOrion.Filters;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Models;
using FlussonnicOrion.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlussonnicOrion.Managers
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
                _dataSource = orionSettings.UseCache ? _scopeFactory.Resolve<OrionCacheDataSource>() : _scopeFactory.Resolve<OrionClientDataSource>();
                _dataSource.Initialize(orionSettings.EmployeesUpdatingInterval, orionSettings.VisitorsUpdatingInterval);
                _accessController = new AccessController(_dataSource);

                var accessPoints = _serviceSettingsController
                    .Settings
                    .AccesspointsSettings
                    .Select(x =>
                        new AccessPointController(
                            x.AccesspointId, 
                            GetFilter(x.FilterType), 
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

        private IFilter GetFilter(FilterType filterType)
        {
            switch (filterType)
            {
                case FilterType.Empty:
                    return new EmptyFilter();
                case FilterType.Crosscam:
                    return new CrossCamerasFilter(_logger, _orionClient, _serviceSettingsController);
                case FilterType.Opentimeout:
                    return new OpenTimeoutFilter(_logger, _orionClient, _serviceSettingsController);
                default: throw new InvalidCastException($"Тип фильтра {filterType} не поддерживается");
            }
        }

        public AccessPointController GetAcceessPoint(int id)
        {
            return _accessPoints.FirstOrDefault(x => x.Id == id);
        }
    }
}
