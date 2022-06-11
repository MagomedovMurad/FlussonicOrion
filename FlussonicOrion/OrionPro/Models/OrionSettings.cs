﻿using System.Collections.Generic;

namespace FlussonicOrion.OrionPro.Models
{
    public class OrionSettings
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string ModuleUserName { get; set; }
        public string ModulePassword { get; set; }
        public string EmployeeUserName { get; set; }
        public string EmployeePassword { get; set; }
        public int TokenLifetime { get; set; }
        public bool UseCache { get; set; }
        public int CacheUpdatingInterval { get; set; }
    }
}
