using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.AppHost.Interop
{
    class WndUtils
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowText(IntPtr hWnd, string windowName);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleTitle(string text);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">
        /// The process <paramref name="handle"/>. 
        /// </param>
        /// <returns>
        /// An instance of the Process class. 
        /// </returns>
        /// <exception cref="Win32Exception">status != 0</exception>
        public static Process GetParentProcess(IntPtr handle)
        {
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}