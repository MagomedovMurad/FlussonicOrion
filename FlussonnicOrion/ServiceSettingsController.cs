using FlussonnicOrion.Flussonic.Models;
using FlussonnicOrion.OrionPro.Models;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net;

namespace FlussonnicOrion
{
    public class ServiceSettingsController
    {
        private const string _fileName = "settings.txt";
        public ServiceSettings Settings { get; set; }

        public void Initialize()
        {
            var exist = File.Exists(_fileName);
            if (exist)
            {
                LoadSettings();
            }
            else
            {
                WriteDefaultSettings();
                LoadSettings();
            }
        }

        public void LoadSettings()
        {
            var stringSettings =  File.ReadAllText(_fileName);
            Settings = JsonConvert.DeserializeObject<ServiceSettings>(stringSettings);
        }

        private void WriteDefaultSettings()
        {
            var defaultSettings = GetDefaultSettings();
            var stringDefaultSettings = JsonConvert.SerializeObject(defaultSettings);
            File.WriteAllText("settings.txt", stringDefaultSettings);
        }
         
        private ServiceSettings GetDefaultSettings()
        {
            var orionSettings = new OrionSettings
            {
                IPAddress = IPAddress.Parse("127.0.0.1"),
                Port = 8090,
                ModuleUserName = "admin",
                ModulePassword = "password",
                EmployeeUserName = "admin123",
                EmployeePassword = "password123",
                TokenLifetime = 300
            };

            var flussonicSettings = new FlussonicSettings
            {
                IPAddress = IPAddress.Parse("127.0.0.1"),
                Port = 80
            };

            var serviceSettings = new ServiceSettings
            {
                OrionSettings = orionSettings,
                FlussonicSettings = flussonicSettings
            };

            return serviceSettings;
        }
    }
}
