using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFPSBoost.Native.Unmanaged
{
    public static class ServiceHelper
    {


        public static void ChangeStartMode(ServiceController svc, ServiceStartMode mode)
        {
            var scManagerHandle = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_ALL_ACCESS);
            if (scManagerHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Manager Error");
            }

            var serviceHandle = NativeMethods.OpenService(
                scManagerHandle,
                svc.ServiceName,
                NativeMethods.SERVICE_QUERY_CONFIG | NativeMethods.SERVICE_CHANGE_CONFIG);

            if (serviceHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Error");
            }

            var result = NativeMethods.ChangeServiceConfig(
                serviceHandle,
                NativeMethods.SERVICE_NO_CHANGE,
                (uint)mode,
                NativeMethods.SERVICE_NO_CHANGE,
                null,
                null,
                IntPtr.Zero,
                null,
                null,
                null,
                null);

            if (result == false)
            {
                int nError = Marshal.GetLastWin32Error();
                var win32Exception = new Win32Exception(nError);
                throw new ExternalException("Could not change service start type: "
                    + win32Exception.Message);
            }

            NativeMethods.CloseServiceHandle(serviceHandle);
            NativeMethods.CloseServiceHandle(scManagerHandle);
        }
    }
}
