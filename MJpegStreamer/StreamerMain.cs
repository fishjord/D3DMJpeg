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
    class StreamerMain
    {

        public static void onMsg(Int32 processId, MessageType type, string msg)
        {
            Trace.TraceWarning("Debug message from process {0} {1}: {2}", processId, type, msg);
        }

        /// <summary>
        /// The callback for when the screenshot has been taken
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="status"></param>
        /// <param name="screenshotResponse"></param>
        public static void processShot(Int32 clientPID, ResponseStatus status, ScreenshotResponse sr)
        {
        }

        static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;
            Trace.Indent();
            
            if (args.Length == 0)
            {
                Console.WriteLine("USAGE: StreamerMain <process name> <target host> <target port> <fps>");
                Console.WriteLine("\nRunning processes:");
                foreach (string s in ScreenCapturer.listAllD3DProcesses())
                {
                    Console.WriteLine(s);
                }

                return;
            }

            string processName = args[0];
            string ffmpegHost = args[1];
            int ffmpegPort = int.Parse(args[2]);
            float fps = float.Parse(args[3]);

            ScreenCapturer screenCap = new ScreenCapturer(processShot);
            ScreenshotManager.OnScreenshotMessage += onMsg;
            screenCap.hook(processName);
            screenCap.sendRequest(new StreamRequest(new Rectangle(0, 0, 0, 0), ffmpegHost, ffmpegPort, fps));

            char k;
            while ((k = Console.ReadKey().KeyChar) != 'q')
            {
                if (k == 'p')
                {
                    Trace.TraceInformation("Pausing capturing");
                    screenCap.sendRequest(new PauseRequest());
                }
                else if (k == 'r')
                {
                    Trace.TraceInformation("Pausing capturing");
                    screenCap.sendRequest(new ResumeRequest());
                }

            }
            Trace.TraceInformation("Stopping screen caputring...");
            screenCap.sendRequest(new StopRequest());
            Thread.Sleep(1000);
            Console.ReadKey();
        }
    }
}