using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Flussonic.Enums;
using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonnicOrion.Controllers
{
    public interface ILogicController
    {
        Task Initialize();
        void Dispose();
    }

    public class LogicController: ILogicController
    {
        private readonly ILogger<LogicController> _logger;

        private IServiceSettingsController _serviceSettingsController;
        private IFlussonic _flussonic;
        private IOrionClient _orionClient;
        private IOrionDataSource _dataSource;
        private IAccesspointsCache _accesspointsCache;
        private IAccessController _accessController;
        private IServiceScopeFactory _scopeFactory; 

        public LogicController(ILogger<LogicController> logger, 
                          IOrionClient orionClient, 
                          IServiceScopeFactory scopeFactory,
                          IServiceSettingsController serviceSettingsController,
                          IAccesspointsCache accesspointsCache)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _orionClient = orionClient;
            _serviceSettingsController = serviceSettingsController;
            _accesspointsCache = accesspointsCache;
        }

        public async Task Initialize()
        {
            try
            {
                _serviceSettingsController.Initialize();

                var orionSettings = _serviceSettingsController.Settings.OrionSettings;
                _accesspointsCache.Initialize(orionSettings.AccesspointsSettings);
                await _orionClient.Initialize(orionSettings);

                _dataSource = orionSettings.UseCache ? _scopeFactory.Resolve<OrionCacheDataSource>() : _scopeFactory.Resolve<OrionClientDataSource>();
                _dataSource.Initialize(orionSettings.EmployeesUpdatingInterval, orionSettings.VisitorsUpdatingInterval);

                _accessController = new AccessController(_dataSource);

                var httpServer = new HttpServer(_serviceSettingsController.Settings.ServerSettings.ServerPort);
                httpServer.Start();

                var helper = new EntryPointsHelper(httpServer, _orionClient, _logger);
                helper.Initialize();

                _flussonic = new FlussonicServer(httpServer,  _logger);
                _flussonic.Start();
                _flussonic.NewEvent += Flussonic_NewEvent;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации службы");
            }
        }

        public void Dispose()
        {
            _flussonic?.Stop();
            _orionClient?.Dispose();
            _dataSource?.Dispose();
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

                    if (e.ObjectAction != ObjectAction.Enter)
                    {
                        _logger.LogWarning($"Объект типа {e.ObjectAction} не поддерживается"); ;
                        return;
                    }

                    var cameraSettings = _accesspointsCache.GetVideosourceSettings(e.CameraId);
                    if (cameraSettings == null)
                    {
                        _logger.LogWarning($"Камера с идентификатором {e.CameraId} не привязана к точке доступа");
                        return;
                    }

                    var accessResults = _accessController.CheckAccess(e.ObjectId, cameraSettings.AccesspointId, cameraSettings.PassageDirection);

                    var allowedAccessResult = accessResults.Where(x => x.AccessAllowed)
                                                           .OrderByDescending(x => x.StartDateTime)
                                                           .FirstOrDefault();
                    foreach (var result in accessResults)
                    {
                        var text = $"Доступ {(result.AccessAllowed ? "разрешен" : "запрещен")}. {result.PersonData}. {result.Reason}";
                        _logger.LogInformation(text);
                    }
                    

                    if (allowedAccessResult != null)
                    {
                        _logger.LogInformation($"Отправка команды на открытие двери {cameraSettings.AccesspointId} для {allowedAccessResult.PersonData}");
                        ActionType actionType = Convert(cameraSettings.PassageDirection);
                        await _orionClient.ControlAccesspoint(cameraSettings.AccesspointId, AccesspointCommand.ProvisionOfAccess, actionType, allowedAccessResult.PersonId);
                    }

                    await AddExternalEvents(accessResults.Except(new[] { allowedAccessResult }).ToList(), cameraSettings.AccesspointId);
                    await AddAdditionalExternalEvents(accessResults, e.ObjectId, cameraSettings.AccesspointId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при обработке номера {e.ObjectId}");
                }
            });
        }

        private ActionType Convert(PassageDirection passageDirection)
        {
            switch (passageDirection)
            {
                case PassageDirection.Enter: 
                    return ActionType.Entry;
                
                case PassageDirection.Exit: 
                    return ActionType.Exit;

                default: throw new InvalidOperationException($"Тип {passageDirection} не поддерживается");
            }
        }

        private async Task AddExternalEvents(IEnumerable<AccessRequestResult> requestResults, int accesspointId)
        {
            foreach (var accessResult in requestResults)
            {
                var eventType = accessResult.AccessAllowed ? EventType.AccessGranted : EventType.AccessDenied;
                await _orionClient.AddExternalEvent(0, accesspointId, ItemType.ACCESSPOINT, (int)eventType, accessResult.KeyId, accessResult.PersonId, null);
            }
        }

        private async Task AddAdditionalExternalEvents(IEnumerable<AccessRequestResult> requestResults, string number, int accesspointId)
        {
            foreach (var accessResult in requestResults)
            {
                var text = $"{number}. {(accessResult.AccessAllowed ? "Доступ" : "Запрет")}. {accessResult.Reason}";// {accessResult.PersonData}";
                await _orionClient.AddExternalEvent(0, accesspointId, ItemType.ACCESSPOINT, (int)EventType.ThirdPartyEvent, accessResult.KeyId, accessResult.PersonId, text);
            }
        }

    }
}
