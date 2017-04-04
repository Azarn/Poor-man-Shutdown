using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Poor_man_s_Shutdown.Windows {
    public struct TOKEN_PRIVILEGES {
        public const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        public const uint SE_PRIVILEGE_REMOVED = 0x00000004;
        public const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        public uint PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = WINNT.ANYSIZE_ARRAY)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

}
