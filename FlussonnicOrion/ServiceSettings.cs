using FlussonnicOrion.Models;
using FlussonnicOrion.OrionPro.Models;
using System.Collections.Generic;

namespace FlussonnicOrion
{
    public class ServiceSettings
    {
        public ServerSettings ServerSettings { get; set; }
        public OrionSettings OrionSettings { get; set; }
        public List<AccesspointSettings> AccesspointsSettings { get; set; }
        public FilterSettings FilterSettings { get; set; }
    }
    public class AccesspointSettings
    {
        public int AccesspointId { get; set; }
        public string EnterCamId { get; set; }
        public string ExitCamId { get; set; }
        public FilterType FilterType { get; set; }
    }

    public enum FilterType
    {
        Empty,
        Crosscam,
        Opentimeout
    }

    public class FilterSettings
    { 
        public int TimeAfterLastAccessGranted { get; set; }
        public int TimeAfterLastPass { get; set; }
        public int EventLogDelay { get; set; }
        public int TimeInFrameForProcessing { get; set; }
    }
}
