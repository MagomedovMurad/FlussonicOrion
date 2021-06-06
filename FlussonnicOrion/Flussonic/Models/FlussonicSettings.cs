using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Flussonic.Models
{
    public class FlussonicSettings
    {
        public bool IsServerMode { get; set; }
        public int ServerPort {get; set;}
        public string WatcherIPAddress { get; set; }
        public int WatcherPort { get; set; }
    }
}
