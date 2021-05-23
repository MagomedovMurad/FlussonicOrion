using FlussonnicOrion.Flussonic.Models;
using FlussonnicOrion.OrionPro.Models;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net;

namespace FlussonnicOrion
{
    public class ServiceSettingsCache
    {
        public void LoadSettings()
        {
            var stringSettings =  File.ReadAllText("settings.txt");
            var settings = JsonConvert.DeserializeObject<ServiceSettings>(stringSettings);
        }

        public void GetDefaultSettings()
        {
            var orionSettings = new OrionSettings
            {
                IPAddress = IPAddress.Parse("127.0.0.1"),
                Port = 8090,
                ModuleUserName = "admin",
                ModulePassword = "password",
                EmployeeUserName = "admin123",
                EmployeePassword = "password123"
            };

            var flussonicSettings = new FlussonicSettings
            {
                
            };
        }
    }
}
