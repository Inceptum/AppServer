using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.Hosting
{
    public class JobObject : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateJobObject(IntPtr a, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, UInt32 cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private extern static Boolean CloseHandle(IntPtr handle);

        private IntPtr m_Handle = IntPtr.Zero;

        public JobObject()
        {
            m_Handle = CreateJobObject(IntPtr.Zero, null);
            if (m_Handle == IntPtr.Zero)
                throw new Exception(string.Format("Failed to create job object. Error: {0}", Marshal.GetLastWin32Error()));

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = 0x2000
            };

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = info
            };

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!SetInformationJobObject(m_Handle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
                throw new Exception(string.Format("Unable to set information.  Error: {0}", Marshal.GetLastWin32Error()));
        }

        ~JobObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            Close();
        }

        public void Close()
        {
            if (m_Handle == IntPtr.Zero) return;
            CloseHandle(m_Handle);
            m_Handle = IntPtr.Zero;
        }

        public bool AddProcess(IntPtr processHandle)
        {
            return AssignProcessToJobObject(m_Handle, processHandle);
        }

        public bool AddProcess(int processId)
        {
            return AddProcess(Process.GetProcessById(processId).Handle);
        }

        public void AddProcess(Process process)
        {
            AddProcess(process.Handle);
        }
    }

    #region Helper classes

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public UInt64 ReadOperationCount;
        public UInt64 WriteOperationCount;
        public UInt64 OtherOperationCount;
        public UInt64 ReadTransferCount;
        public UInt64 WriteTransferCount;
        public UInt64 OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public Int64 PerProcessUserTimeLimit;
        public Int64 PerJobUserTimeLimit;
        public UInt32 LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public UInt32 ActiveProcessLimit;
        public UIntPtr Affinity;
        public UInt32 PriorityClass;
        public UInt32 SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public UInt32 nLength;
        public IntPtr lpSecurityDescriptor;
        public Int32 bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    public enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }

    #endregion
}