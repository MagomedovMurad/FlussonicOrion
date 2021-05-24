﻿using System;
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
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
    }
}
