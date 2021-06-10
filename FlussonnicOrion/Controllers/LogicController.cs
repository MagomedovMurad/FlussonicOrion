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
        private IOrionCache _orionCache;
        private IAccessController _accessController;
        private IAccesspointsCache _accesspointsCache;

        public LogicController(ILogger<LogicController> logger, 
                          IOrionClient orionClient, 
                          IOrionCache orionCache, 
                          IServiceScopeFactory scopeFactory,
                          IServiceSettingsController serviceSettingsController,
                          IAccesspointsCache accesspointsCache)
        {
            _logger = logger;
            _orionClient = orionClient;
            _orionCache = orionCache;
            _accessController = scopeFactory.Resolve<IAccessController>();
            _serviceSettingsController = serviceSettingsController;
            _accesspointsCache = accesspointsCache;
        }

        public async Task Initialize()
        {
            _serviceSettingsController.Initialize();

            var orionSettings = _serviceSettingsController.Settings.OrionSettings;
            _accesspointsCache.Initialize(orionSettings.AccesspointsSettings);
            await _orionClient.Initialize(orionSettings);
            _orionCache.Initialize(orionSettings.EmployeesUpdatingInterval, orionSettings.VisitorsUpdatingInterval);

            var flussonicSettings = _serviceSettingsController.Settings.FlussonicSettings;
            _flussonic = flussonicSettings.IsServerMode ? new FlussonicServer(flussonicSettings.ServerPort, _logger) : 
                                        new FlussonicClient(flussonicSettings.WatcherIPAddress,
                                                            flussonicSettings.WatcherPort);
            _flussonic.Start();
            _flussonic.NewEvent += Flussonic_NewEvent;
        }

        public void Dispose()
        {
            _flussonic?.Stop();
            _orionClient?.Dispose();
            _orionCache?.Dispose();
        }        

        private void Flussonic_NewEvent(object sender, FlussonicEvent e)
        {
            Task.Run(async () =>
            {
                _logger.LogInformation($"Новое событие от камеры: {e.CameraId}. Гос. номер: {e.ObjectId}. Action: {e.ObjectAction}");
                if (e.ObjectClass != ObjectClass.Vehicle)
                    return;

                if (e.ObjectAction != ObjectAction.Enter)
                    return;

                var cameraSettings = _accesspointsCache.GetVideosourceSettings(e.CameraId);
                if (cameraSettings == null)
                    return;

                var accessResults = _accessController.CheckAccess(e.ObjectId, cameraSettings.AccesspointId);

                var allowedAccessResult = accessResults.Where(x => x.AccessAllowed)
                                                       .OrderByDescending(x => x.StartDateTime)
                                                       .FirstOrDefault();

                if (allowedAccessResult != null)
                {
                    ActionType actionType = Convert(cameraSettings.PassageDirection);
                    await _orionClient.ControlAccesspoint(cameraSettings.AccesspointId, AccesspointCommand.ProvisionOfAccess, actionType, allowedAccessResult.PersonId);
                }

                await AddExternalEvents(accessResults.ToList().Except(new[] { allowedAccessResult }), cameraSettings.AccesspointId);
                await AddAdditionalExternalEvents(accessResults, e.ObjectId, cameraSettings.AccesspointId);
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
