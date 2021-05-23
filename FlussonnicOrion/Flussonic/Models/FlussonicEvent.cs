using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Models
{
    public class FlussonicEvent
    {
        public Guid Id { get; set; }
        public int StartAt { get; set; }
        public int EndAt { get; set; }
        public FlussonicEventType Type { get; set; }
        public string CameraId { get; set; }
        public string ObjectId { get; set; }
        public string ExtData { get; set; }
        public string Source { get; set; }
        public int SourceId { get; set; }
        public string ObjectClass { get; set; }
        public string EventData { get; set; }
    }
}
