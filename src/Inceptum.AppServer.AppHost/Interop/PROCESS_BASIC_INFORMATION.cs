using System;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.AppHost.Interop
{
    /// <summary>
    /// The process basic information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
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
    }
}