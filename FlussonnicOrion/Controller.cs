using FlussonnicOrion.Flussonic;
using FlussonnicOrion.OrionPro;
using FlussonnicOrion.OrionPro.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Controller
    {
        private ServiceSettingsController _serviceSettingsController;
        private IFlussonic _flussonic;
        private OrionClient _orion;

        private Dictionary<string, int> _camerasToBariers;

        public Controller()
        {
        }

        private async Task Initialize()
        {
            _serviceSettingsController = new ServiceSettingsController();
            _serviceSettingsController.Initialize();

            var settings = _serviceSettingsController.Settings;
            _camerasToBariers = settings.FlussonicSettings.CamToBarier;
            var isServerMode = settings.FlussonicSettings.IsServerMode;

            _orion = new OrionClient();
            await _orion.Initialize(_serviceSettingsController.Settings.OrionSettings);

            _flussonic = isServerMode ? new FlussonicServer(settings.FlussonicSettings.ServerPort) : 
                                        new FlussonicClient(settings.FlussonicSettings.WatcherIPAddress, settings.FlussonicSettings.WatcherPort);
            _flussonic.Start();
            _flussonic.NewEvent += Flussonic_NewEvent;

            //(IPAddress.Parse("172.20.5.51"), 8090, userName: "admin", password: "password", tokenLogin: "admin123", tokenPassword: "password");
        }

        private void Flussonic_NewEvent(object sender, Models.FlussonicEvent e)
        {
            //TODO: отфильтровать по типу события и т.д

            var isSuccess = _camerasToBariers.TryGetValue(e.CameraId, out int value);
            if (!isSuccess)
                return;

            //TODO: определить имеет ли человек доступ

            _orion.ControlAccesspoint(value, AccesspointCommand.ProvisionOfAccess, ActionType.Passage, );
            _orion.AddExternalEvent(б);
        }
    }
}
