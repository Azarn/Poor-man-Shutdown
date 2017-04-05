using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using Poor_man_s_Shutdown.Windows;

namespace Poor_man_s_Shutdown {
    public class Program {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InitiateSystemShutdownEx(string lpMachineName, string lpMessage,
                                                            uint dwTimeout, bool bForceAppsClosed,
                                                            bool bRebootAfterShutdown, ShutdownReason dwReason);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint Zero,
            IntPtr Null1,
            IntPtr Null2);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        private static bool ObtainShutdownPrivilege() {
            LUID luid = new LUID();
            if (!LookupPrivilegeValue(null, WINNT.SE_SHUTDOWN_NAME, out luid)) {
                Console.WriteLine("LookupPrivilegeValue error: {0}", Marshal.GetLastWin32Error());
                return false;
            }

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
            tp.PrivilegeCount = 1;
            tp.Privileges = new LUID_AND_ATTRIBUTES[1] {
                new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = WINNT.SE_PRIVILEGE_ENABLED }
            };

            IntPtr hToken = IntPtr.Zero;
            if (!OpenProcessToken(GetCurrentProcess(), WINNT.TOKEN_ADJUST_PRIVILEGES | WINNT.TOKEN_QUERY, out hToken)) {
                Console.WriteLine("OpenProcessToken error: {0}", Marshal.GetLastWin32Error());
                return false;
            }

            if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)) {
                Console.WriteLine("AdjustTokenPrivileges error: {0}", Marshal.GetLastWin32Error());
                return false;
            }
            return true;
        }

        private static void PrintHelp(OptionSet p, string message = null) {
            string myName = AppDomain.CurrentDomain.FriendlyName;
            if (message != null) {
                Console.WriteLine("{0}: {1}", myName, message);
            }

            Console.WriteLine("Usage: {0} [OPTIONS]+ message", myName);
            Console.WriteLine("Sample program for shutdown/logoff operations.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        public static void Main(string[] args) {
            ShutdownReason reason = ShutdownReason.FlagPlanned | ShutdownReason.MajorOther | ShutdownReason.MinorOther;

            bool showHelp = false;
            bool logoff = false;
            bool isRebooting = false;
            bool shutdown = false;
            bool isForced = false;
            uint timeout = 20;

            OptionSet p = new OptionSet() {
                { "l", "Logs off the current user/", v => logoff = v != null },
                { "r", "Reboots after shutdown.", v => isRebooting = v != null },
                { "s", "Shuts down the local computer.", v => shutdown = v != null },
                { "f", "Forces running applications to close.", v => isForced = v != null},
                { "t=", "Sets the timer for system shutdown in {NUMBER} of seconds. The default is 20 seconds.", (uint v) => timeout = v },
                { "h",  "Show this message and exit.", v => showHelp = v != null },
            };

            List<string> extra;
            try {
                extra = p.Parse(args);
            } catch (OptionException e) {
                PrintHelp(p, e.Message);
                return;
            }

            if (extra.Count != 0 || showHelp || !(shutdown || logoff) || (logoff && (isRebooting || shutdown))) {
                PrintHelp(p);
                return;
            }
            
            if (logoff) {
                ExitWindows flags = ExitWindows.LogOff | (isForced ? ExitWindows.Force : ExitWindows.ForceIfHung);
                if (!ExitWindowsEx(flags, reason)) {
                    Console.WriteLine("ExitWindowsEx error: {0}", Marshal.GetLastWin32Error());
                }
                return;
            }

            if (shutdown) {
                if (!ObtainShutdownPrivilege()) {
                    return;
                }

                if (!InitiateSystemShutdownEx(null, "Gotcha!", timeout, isForced, isRebooting, reason)) {
                    Console.WriteLine("InitiateSystemShutdownEx error: {0}", Marshal.GetLastWin32Error());
                    return;
                }
            }
        }
    }
}
