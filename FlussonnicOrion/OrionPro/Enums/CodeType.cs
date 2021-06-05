using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.OrionPro.Enums
{
    public enum CodeType
    {
        /// <summary>
        /// Пароль для программ
        /// </summary>
        ProgramsPassword = 1,

        /// <summary>
        ///  Пин-код
        /// </summary>
        PinCode = 2,

        /// <summary>
        ///  Брелок TochMemory
        /// </summary>
        TochMemoryKeychain = 3,

        /// <summary>
        ///  Proxy-карта
        /// </summary>
        ProxyCard = 4,

        /// <summary>
        /// Автомобильный номер
        /// </summary>
        CarNumber = 5,

        /// <summary>
        /// Отпечаток пальца
        /// </summary>
        Fingerprint = 7,

        /// <summary>
        /// Отпечаток лица
        /// </summary>
        FacePrint = 8,

        /// <summary>
        /// Отпечаток ладонь
        /// </summary>
        PalmPrint = 9,

        /// <summary>
        ///  Пин-код2
        /// </summary>
        PinCode2 = 12,

        /// <summary>
        /// Отпечаток лица S1007
        /// </summary>
        FacePrintS1007 = 16
    }
}
