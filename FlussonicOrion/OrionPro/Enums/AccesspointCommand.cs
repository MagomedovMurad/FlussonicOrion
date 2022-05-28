using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonicOrion.OrionPro.Enums
{
    public enum AccesspointCommand
    {
        /// <summary>
        /// Предоставление доступа
        /// </summary>
        ProvisionOfAccess = 0,

        /// <summary>
        /// Разрешение (восстановление) доступа
        /// </summary>
        PermissionAccess = 1,

        /// <summary>
        /// Разрешение входа
        /// </summary>
        LoginPermission = 2,

        /// <summary>
        /// Разрешение выхода
        /// </summary>
        OutputResolution = 3,

        /// <summary>
        ///  Запрет доступа (входа и выхода);
        /// </summary>
        AccessDenial = 4,

        /// <summary>
        /// Запрет входа
        /// </summary>
        EntryBarring = 5,

        /// <summary>
        /// Запрет выхода
        /// </summary>
        ExitBan = 6,

        /// <summary>
        /// Открытие доступа
        /// </summary>
        OpeningAccess
    }
}
