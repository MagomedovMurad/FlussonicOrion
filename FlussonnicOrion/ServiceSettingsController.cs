using FlussonnicOrion.Flussonic.Models;
using FlussonnicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FlussonnicOrion
{
    public interface IServiceSettingsController
    {
        ServiceSettings Settings { get; }
        ServiceSettings Initialize();
        bool LoadSettings();
    }

    public class ServiceSettingsController: IServiceSettingsController
    {
        private ILogger<IServiceSettingsController> _logger;
        private const string _fileName = "settings.txt";
        public ServiceSettings Settings { get; set; }

        public ServiceSettingsController(ILogger<IServiceSettingsController> logger)
        {
            _logger = logger;
        }

        public ServiceSettings Initialize()
        {
            var exist = File.Exists(_fileName);
  
            if(!exist)
                WriteDefaultSettings();

            bool success = LoadSettings();
            if (!success)
            {
                Settings = GetDefaultSettings();
                _logger.LogError("Не удалось загрузить настройки из конфигурационного файла. Используются настройки по умолчанию");
            }

            return Settings;
        }

        public bool LoadSettings()
        {
            try
            {
                var stringSettings = File.ReadAllText(_fileName);
                Settings = JsonConvert.DeserializeObject<ServiceSettings>(stringSettings);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при чтении настроек из файла конфигурации");
                return false;
            }
        }

        private void WriteDefaultSettings()
        {
            try
            {
                var defaultSettings = GetDefaultSettings();
                var stringDefaultSettings = JsonConvert.SerializeObject(defaultSettings);
                File.WriteAllText("./settings.txt", stringDefaultSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании конфигурационного файла с настройками по умолчанию");
            }
        }
         
        private ServiceSettings GetDefaultSettings()
        {
            var orionSettings = new OrionSettings
            {
                IPAddress = "127.0.0.1",
                Port = 8090,
                ModuleUserName = "admin",
                ModulePassword = "password",
                EmployeeUserName = "admin123",
                EmployeePassword = "password123",
                TokenLifetime = 300,
                EmployeesUpdatingInterval = 60,
                VisitorsUpdatingInterval = 60,
                VideSourceToAccessPoint = new Dictionary<string, int>
                {
                    {"cam1", 1 },
                    {"cam2", 2 },
                }
            };

            var flussonicSettings = new FlussonicSettings
            {
                IsServerMode = true,
                ServerPort = 26038,
                WatcherIPAddress = "127.0.0.1",
                WatcherPort= 80
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
