using FlussonnicOrion.OrionPro.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion
{
    public interface IAccesspointsCache
    {
        void Initialize(List<AccesspointSettings> accesspointSettings);
        VideosourceSettings GetVideosourceSettings(string cameraId);
    }

    public class AccesspointsCache: IAccesspointsCache
    {
        private ILogger<IAccesspointsCache> _logger;
        private List<VideosourceSettings> _accesspointSettings;
        public AccesspointsCache(ILogger<IAccesspointsCache> logger)
        {
            _logger = logger;
        }

        public void Initialize(List<AccesspointSettings> accesspointSettings)
        {
            try
            {
                _accesspointSettings = accesspointSettings.SelectMany(x =>
                {
                    var enter = new VideosourceSettings
                    {
                        AccesspointId = x.AccesspointId,
                        VideosourceId = x.EnterCamId,
                        PassageDirection = PassageDirection.Enter
                    };

                    var exit = new VideosourceSettings
                    {
                        AccesspointId = x.AccesspointId,
                        VideosourceId = x.ExitCamId,
                        PassageDirection = PassageDirection.Exit
                    };
                    return new[] { enter, exit };
                }).ToList();
                _logger.LogInformation("AccesspointsCache инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации AccesspointsCache");
            }
        }

        public VideosourceSettings GetVideosourceSettings(string cameraId)
        {
            try
            {
                return _accesspointSettings.SingleOrDefault(x => x.VideosourceId == cameraId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Камера {cameraId} привязана к нескольким точкам доступа");
                var settings = _accesspointSettings.First(x => x.VideosourceId == cameraId);
                settings.PassageDirection = PassageDirection.Enter;
                return settings;
            }
        }
    }

    public class VideosourceSettings
    { 
        public string VideosourceId { get; set; }
        public int AccesspointId { get; set; }
        public PassageDirection PassageDirection { get; set; }
    }
         

    public enum PassageDirection
    { 
        Enter,
        Exit
    }
}
