using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Capture.Interface;
using Capture.Hook;
using Capture;
using EasyHook;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Concurrent;

namespace MJpegStreamer
{
    class StreamerMain
    {
        int processId = 0;
        Process _process;
        CaptureProcess _captureProcess;

        private ConcurrentQueue<Image> queue;
        private HttpStreamer streamer;

        public StreamerMain()
        {
            queue = new ConcurrentQueue<Image>();
            streamer = new HttpStreamer(8080, queue);
        }

        public void DetachProcess()
        {
            if (_captureProcess != null)
            {
                HookManager.RemoveHookedProcess(_captureProcess.Process.Id);
                _captureProcess.CaptureInterface.Disconnect();
                _captureProcess = null;
            }
        }

        public void AttachProcess(String exeName, bool gac)
        {
            DetachProcess();

            if (gac)
            {
                // NOTE: On some 64-bit setups this doesn't work so well.
                //       Sometimes if using a 32-bit target, it will not find the GAC assembly
                //       without a machine restart, so requires manual insertion into the GAC
                // Alternatively if the required assemblies are in the target applications
                // search path they will load correctly.

                // Must be running as Administrator to allow dynamic registration in GAC
                Config.Register("Capture",
                    "Capture.dll");
            }

            Process[] processes = Process.GetProcessesByName(exeName);
            foreach (Process process in processes)
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    continue;
                }

                Direct3DVersion direct3DVersion = Direct3DVersion.AutoDetect;

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true,
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);
                _captureProcess = new CaptureProcess(process, cc, captureInterface);

                break;
            }

            Thread.Sleep(10);
            _captureProcess.BringProcessWindowToFront();
        }

        public void ShutdownStreamer()
        {
            streamer.Shutdown();
        }

        /// <summary>
        /// Display messages from the target process
        /// </summary>
        /// <param name="message"></param>
        void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            if (message.MessageType != MessageType.Debug)
            {
                Trace.WriteLine(message.MessageType + ": " + message.Message);
            }
        }

        /// <summary>
        /// Display debug messages from the target process
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="message"></param>
        void ScreenshotManager_OnScreenshotDebugMessage(int clientPID, string message)
        {
            Trace.WriteLine("DEBUG(" + clientPID + "): " + message);
        }

        /// <summary>
        /// Create the screen shot request
        /// </summary>
        public void DoRequest()
        {
            if (streamer.HasConnections)
            {
                // Initiate the screenshot of the CaptureInterface, the appropriate event handler within the target process will take care of the rest
                _captureProcess.CaptureInterface.BeginGetScreenshot(Rectangle.Empty, new TimeSpan(0, 0, 2), Callback);
            }
        }

        /// <summary>
        /// The callback for when the screenshot has been taken
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="status"></param>
        /// <param name="screenshotResponse"></param>
        void Callback(IAsyncResult result)
        {
            if (_captureProcess == null)
            {
                return;
            }

            using (Screenshot screenshot = _captureProcess.CaptureInterface.EndGetScreenshot(result))
            {
                try
                {
                    _captureProcess.CaptureInterface.DisplayInGameText("Screenshot captured...");
                    if (screenshot != null && screenshot.CapturedBitmap != null)
                    {
                        queue.Enqueue(Image.FromStream(new MemoryStream(screenshot.CapturedBitmap)));
                        CaptureInterface_RemoteMessage(new MessageReceivedEventArgs(MessageType.Information, "Images in queue= " + queue.Count));
                    }
                }
                catch
                {
                }
            }
        }
    }
}