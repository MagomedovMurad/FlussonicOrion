using FlussonnicOrion.Flussonic;
using FlussonnicOrion.OrionPro;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Controller
    {
        private ServiceSettingsController _serviceSettingsController;
        private IFlussonic _flussonic;
        private OrionClient _orion;
        public Controller()
        {
        }

        private async Task Initialize()
        {
            _serviceSettingsController = new ServiceSettingsController();
            _serviceSettingsController.Initialize();
            var isServerMode = _serviceSettingsController.Settings.FlussonicSettings.IsServerMode;

            _flussonic = isServerMode ? new FlussonicServer() : new FlussonicClient();
            _orion = new OrionClient();

            _serviceSettingsController.Initialize();
            _flussonic.Start();
            _flussonic.NewEvent += Flussonic_NewEvent;
            await _orion.Initialize(_serviceSettingsController.Settings.OrionSettings);//(IPAddress.Parse("172.20.5.51"), 8090, userName: "admin", password: "password", tokenLogin: "admin123", tokenPassword: "password");
        }

        private void Flussonic_NewEvent(object sender, Models.FlussonicEvent e)
        {
            
        }
    }
}
