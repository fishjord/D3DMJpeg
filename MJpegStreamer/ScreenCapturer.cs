using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using ScreenshotInterface;
using EasyHook;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace D3DFrameStreamer
{
    class ScreenCapturer
    {        
        private String ChannelName = null;
        private IpcServerChannel ScreenshotServer;
        private Process hookedProcess;
        private ScreenshotRequestResponseNotification notify;

        public ScreenCapturer(ScreenshotRequestResponseNotification notify)
        {
            // Initialise the IPC server
            ScreenshotServer = RemoteHooking.IpcCreateServer<ScreenshotInterface.ScreenshotInterface>(
                ref ChannelName,
                WellKnownObjectMode.Singleton);

            Trace.TraceInformation("IPC server started, channel ref: {0}", ChannelName);
            this.notify = notify;
        }

        public void sendRequest(ScreenshotRequest request)
        {
            ScreenshotManager.AddScreenshotRequest(hookedProcess.Id, request, notify);
        }

        public bool hook(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            Trace.TraceInformation("Trying to hook process '{0}', {1} matching processes", processName, processes);
            if (processes.Length == 0)
            {
                Trace.TraceError("No target process found");
                return false;
            }

            hookedProcess = processes[0];
            if (hookedProcess.MainWindowHandle == IntPtr.Zero)
            {
                Trace.TraceError("Target process's main window handle is null");
                return false;
            }

            Trace.TraceInformation("Injecting dll in to {0}", hookedProcess.MainWindowTitle);
            Trace.TraceInformation("Screenshot dll location {0}", typeof(ScreenshotInject.ScreenshotInjection).Assembly.Location);

            try
            {
                // Inject DLL into target process
                RemoteHooking.Inject(
                    hookedProcess.Id,
                    InjectionOptions.Default,
                    typeof(ScreenshotInject.ScreenshotInjection).Assembly.Location,//"ScreenshotInject.dll", // 32-bit version (the same because AnyCPU) could use different assembly that links to 32-bit C++ helper dll
                    typeof(ScreenshotInject.ScreenshotInjection).Assembly.Location, //"ScreenshotInject.dll", // 64-bit version (the same because AnyCPU) could use different assembly that links to 64-bit C++ helper dll
                    // the optional parameter list...
                    ChannelName, // The name of the IPC channel for the injected assembly to connect to
                    Direct3DVersion.AutoDetect.ToString() // The direct3DVersion used in the target application
                );
            }
            catch(Exception e)
            {
                Trace.TraceInformation("Failed inject in to target process\r\n" + e);
                return false;
            }
            HookManager.AddHookedProcess(hookedProcess.Id);

            /*Trace.TraceInformation("Bringing process to the front");
            BringProcessWindowToFront(hookedProcess);*/

            return true;
        }

        public static List<string> listAllD3DProcesses()
        {
            List<string> ret = new List<string>();

            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (p.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }
                ret.Add(p.ProcessName);
            }

            return ret;
        }

        public void close()
        {
        }

        /// <summary>
        /// Bring the target window to the front and wait for it to be visible
        /// </summary>
        /// <remarks>If the window does not come to the front within approx. 30 seconds an exception is raised</remarks>
        private void BringProcessWindowToFront(Process process)
        {
            if (process == null)
                return;
            IntPtr handle = process.MainWindowHandle;
            int i = 0;

            while (!NativeMethods.IsWindowInForeground(handle))
            {
                if (i == 0)
                {
                    // Initial sleep if target window is not in foreground - just to let things settle
                    Thread.Sleep(250);
                }

                if (NativeMethods.IsIconic(handle))
                {
                    // Minimized so send restore
                    NativeMethods.ShowWindow(handle, NativeMethods.WindowShowStyle.Restore);
                }
                else
                {
                    // Already Maximized or Restored so just bring to front
                    NativeMethods.SetForegroundWindow(handle);
                }
                Thread.Sleep(250);

                // Check if the target process main window is now in the foreground
                if (NativeMethods.IsWindowInForeground(handle))
                {
                    // Leave enough time for screen to redraw
                    Thread.Sleep(1000);
                    return;
                }

                // Prevent an infinite loop
                if (i > 120) // about 30secs
                {
                    throw new Exception("Could not set process window to the foreground");
                }
                i++;
            }
        }

    }
}
