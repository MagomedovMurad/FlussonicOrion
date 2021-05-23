using FlussonnicOrion.Flussonic;
using FlussonnicOrion.OrionPro;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public class Controller
    {
        private IFlussonic _flussonic;
        private OrionClient _orion;
        private ServiceSettingsCache _serviceSettingsCache;
        public Controller()
        { 
        
        }

        private async Task Initialize()
        {
            _flussonic.Start();
            _flussonic.NewEvent += Flussonic_NewEvent;
            await _orion.Initialize(IPAddress.Parse("172.20.5.51"), 8090, userName: "admin", password: "password", tokenLogin: "admin123", tokenPassword: "password");

        }

        private void Flussonic_NewEvent(object sender, Models.FlussonicEvent e)
        {
            
        }
    }
}
