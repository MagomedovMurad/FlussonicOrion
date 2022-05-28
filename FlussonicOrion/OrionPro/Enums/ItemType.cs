using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro.Enums
{
    public enum ItemType
    {
        /// <summary>
        /// Раздел
        /// </summary>
        SECTION,

        /// <summary>
        /// Вход (шлейф)
        /// </summary>
        LOOP,

        /// <summary>
        /// Устройство
        /// </summary>
        DEVICE,

        /// <summary>
        /// Считыватель
        /// </summary>
        READER,

        /// <summary>
        /// Выход (реле)
        /// </summary>
        RELAY,

        /// <summary>
        /// Зона доступа
        /// </summary>
        ACCESSZONE,

        /// <summary>
        /// Точка доступа (дверь)
        /// </summary>
        ACCESSPOINT,

        /// <summary>
        /// Группа разделов
        /// </summary>
        SECTIONGROUP
    }
}
