using FlussonicOrion.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlussonicOrion.Managers
{
    public interface IVideoSourceManager
    {
        void Initialize(IEnumerable<AccesspointSettings> settings);
        VideoSource GetVideoSource(string cameraId);
    }

    public class VideoSourceManager: IVideoSourceManager
    {
        private readonly ILogger<VideoSourceManager> _logger;
        private readonly List<VideoSource> _videoSources;

        public VideoSourceManager(ILogger<VideoSourceManager> logger)
        {
            _logger = logger;
            _videoSources = new List<VideoSource>();
        }

        public void Initialize(IEnumerable<AccesspointSettings> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    var enter = new VideoSource(setting.EnterCamId, setting.AccesspointId, PassageDirection.Entry);
                    var exit = new VideoSource(setting.ExitCamId, setting.AccesspointId, PassageDirection.Exit);
                    _videoSources.Add(enter);
                    _videoSources.Add(exit);
                }

                _logger.LogInformation("VideoSourceManager инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации VideoSourceManager");
            }
        }

        public VideoSource GetVideoSource(string cameraId)
        {
            try
            {
                return _videoSources.SingleOrDefault(x => x.Id == cameraId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Камера {cameraId} привязана к нескольким точкам доступа");
                return _videoSources.FirstOrDefault(x => x.Id == cameraId);
            }
        }
    }
}
