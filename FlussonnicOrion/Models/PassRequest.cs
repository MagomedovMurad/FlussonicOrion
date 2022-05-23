using System;

namespace FlussonnicOrion.Models
{
    public class PassRequest
    {
        public string LicensePlate { get; set; }
        public PassageDirection Direction { get; set; }
        public DateTime EnterInFrameTime { get; set; }
        public DateTime? OutFrameTime { get; set; }
    }
}
