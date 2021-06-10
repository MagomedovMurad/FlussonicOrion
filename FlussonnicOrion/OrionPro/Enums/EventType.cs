using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.OrionPro.Enums
{
    public enum EventType
    {
        /// <summary>
        /// Событие от сторонней системы
        /// </summary>
        ThirdPartyEvent = 1651,

        /// <summary>
        /// Доступ отклонен
        /// </summary>
        AccessDenied = 26,

        /// <summary>
        /// Доступ предоставлен
        /// </summary>
        AccessGranted = 28
    }
}
