using FlussonicOrion.Models;
using FlussonicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FlussonicOrion
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
        private string _fileName = $"{AppContext.BaseDirectory}settings.json";
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
                File.WriteAllText(_fileName, stringDefaultSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании конфигурационного файла с настройками по умолчанию");
            }
        }
         
        private ServiceSettings GetDefaultSettings()
        {
            var serverSettings = new ServerSettings
            {
                ServerPort = 26038
            };

            var orionSettings = new OrionSettings
            {
                IPAddress = "127.0.0.1",
                Port = 8090,
                ModuleUserName = "admin",
                ModulePassword = "password",
                EmployeeUserName = "admin123",
                EmployeePassword = "password123",
                TokenLifetime = 300,
                CacheUpdatingInterval = 60,
                UseCache = false
            };

            var accesspointsSettings = new List<AccesspointSettings>
            {
                new AccesspointSettings
                {
                    AccesspointId = 1,
                    EnterCamId = "cam1",
                    ExitCamId = "cam2"
                },
                new AccesspointSettings
                {
                    AccesspointId = 2,
                    EnterCamId = "cam3",
                    ExitCamId = "cam4"
                }
            };

            var filterSettings = new FilterSettings
            {
                TimeAfterLastAccessGranted = 20,
                TimeAfterLastPass = 5,
                TimeInFrameForProcessing = 5,
                EventLogDelay = 3
            };

            var serviceSettings = new ServiceSettings
            {
                ServerSettings = serverSettings,
                OrionSettings = orionSettings,
                AccesspointsSettings = accesspointsSettings,
                FilterSettings = filterSettings
            };

            return serviceSettings;
        }
    }
}
