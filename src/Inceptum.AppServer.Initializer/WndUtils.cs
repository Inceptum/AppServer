using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.Initializer
{
    public class WndUtils
    {
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">
        /// The process handle. 
        /// </param>
        /// <returns>
        /// An instance of the Process class. 
        /// </returns>
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

    /// <summary>
    /// The proces s_ basi c_ information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_BASIC_INFORMATION
    {
        // These members must match PROCESS_BASIC_INFORMATION
        /// <summary>
        ///   The reserved 1.
        /// </summary>
        internal IntPtr Reserved1;

        /// <summary>
        ///   The peb base address.
        /// </summary>
        internal IntPtr PebBaseAddress;

        /// <summary>
        ///   The reserved 2_0.
        /// </summary>
        internal IntPtr Reserved2_0;

        /// <summary>
        ///   The reserved 2_1.
        /// </summary>
        internal IntPtr Reserved2_1;

        /// <summary>
        ///   The unique process id.
        /// </summary>
        internal IntPtr UniqueProcessId;

        /// <summary>
        ///   The inherited from unique process id.
        /// </summary>
        internal IntPtr InheritedFromUniqueProcessId;

        /// <summary>
        /// The nt query information process.
        /// </summary>
        /// <param name="processHandle">
        /// The process handle. 
        /// </param>
        /// <param name="processInformationClass">
        /// The process information class. 
        /// </param>
        /// <param name="processInformation">
        /// The process information. 
        /// </param>
        /// <param name="processInformationLength">
        /// The process information length. 
        /// </param>
        /// <param name="returnLength">
        /// The return length. 
        /// </param>
        /// <returns>
        /// The nt query information process. 
        /// </returns>
     

    }
}