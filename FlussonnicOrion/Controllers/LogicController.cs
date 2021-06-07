using FlussonnicOrion.Flussonic;
using FlussonnicOrion.Flussonic.Enums;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using FlussonnicOrion.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        private Dictionary<string, int> _camerasToBariers;

        public LogicController(ILogger<LogicController> logger, 
                          IOrionClient orionClient, 
                          IOrionCache orionCache, 
                          IServiceScopeFactory scopeFactory,
                          IServiceSettingsController serviceSettingsController)
        {
            _logger = logger;
            _orionClient = orionClient;
            _orionCache = orionCache;
            _accessController = scopeFactory.Resolve<IAccessController>();
            _serviceSettingsController = serviceSettingsController;
        }

        public async Task Initialize()
        {
            _serviceSettingsController.Initialize();

            var orionSettings = _serviceSettingsController.Settings.OrionSettings;
            _camerasToBariers = orionSettings.VideSourceToAccessPoint;
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

        private void Flussonic_NewEvent(object sender, Models.FlussonicEvent e)
        {
            Task.Run(async () =>
            {
                _logger.LogInformation($"Новое событие от камеры: {e.CameraId}. Гос. номер: {e.ObjectId}. Action: {e.ObjectAction}");
                if (e.ObjectClass != ObjectClass.Vehicle)
                    return;

                if (e.ObjectAction != ObjectAction.Enter)
                    return;

                var isSuccess = _camerasToBariers.TryGetValue(e.CameraId, out int itemId);
                if (!isSuccess)
                    return;

                var accessResults = _accessController.CheckAccess(e.ObjectId, itemId);
                var allowedAccessResult = accessResults.FirstOrDefault(x => x.AccessAllowed);

                if (allowedAccessResult != null)
                    await _orionClient.ControlAccesspoint(itemId, AccesspointCommand.ProvisionOfAccess, ActionType.Passage, allowedAccessResult.PersonId);

                foreach (var accessResult in accessResults)
                {
                    var text = $"{e.ObjectId}. {(accessResult.AccessAllowed ? "Доступ" : "Запрет")}. {accessResult.Reason}";// {accessResult.PersonData}";
                    await _orionClient.AddExternalEvent(0, itemId, ItemType.ACCESSPOINT, 1651, accessResult.KeyId, accessResult.PersonId, text);
                }
            });
        }
    }
}
