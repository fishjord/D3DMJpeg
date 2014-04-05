using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MJpegStreamer
{
    class Program
    {
        private string processName;
        private float fps;
        private Boolean shutdown = false;

        public Program(string processName, float fps)
        {
            this.processName = processName;
            this.fps = fps;
        }

        public void MainLoop()
        {

            StreamerMain app = new StreamerMain();
            app.AttachProcess(processName, false);

            while (!shutdown)
            {
                    app.DoRequest();
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000 / fps));
            }
            Trace.TraceInformation("Stopping screen caputring...");
            app.DetachProcess();
            app.ShutdownStreamer();
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

        static void Main(string[] args)
        {
            args = new string[] { "Wow-64", "30" };
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;
            Trace.Indent();

            if (args.Length != 2)
            {
                Console.WriteLine("USAGE: StreamerMain <process name> <fps>");
                Console.WriteLine("\nRunning processes:");
                foreach (string s in listAllD3DProcesses())
                {
                    Console.WriteLine(s);
                }
                Console.ReadKey();

                return;
            }

            string processName = args[0];
            float fps = float.Parse(args[1]);

            Program p = new Program(processName, fps);

            Thread t = new Thread(new ThreadStart(p.MainLoop));
            t.Start();

            ConsoleKeyInfo read;
            Console.WriteLine("Press 's' to shutdown");
            do
            {
                read = Console.ReadKey();
            } while (read.KeyChar != 's');

            p.shutdown = true;

            t.Join();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        /*
        private static Image generateImages(Color c)
        {
            Bitmap bmp = new Bitmap(640, 480);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.FillRectangle(new SolidBrush(c), 0, 0, 640, 480);
                }

                return bmp;
        }
         
        static void Main(string[] args)
        {
            List<Image> images = new List<Image>();
            foreach (KnownColor c in Enum.GetValues(typeof(KnownColor)))
            {
                images.Add(generateImages(Color.FromKnownColor(c)));
                if (images.Count > 100)
                {
                    break;
                }
            }
            ConcurrentQueue<Image> queue = new ConcurrentQueue<Image>();

            HttpStreamer streamer = new HttpStreamer(8080, queue);
            
            Random rand = new Random();
            for (int i = 0; i < 10000; i++)
            {
                int frame = rand.Next(images.Count - 1);

                queue.Enqueue(images[frame]);
                Console.WriteLine("Sending frame " + i + ", still: " + frame + " " + queue.Count);
                Thread.Sleep(100);
            }

            streamer.Shutdown();

            Console.ReadKey();
        }*/
    }
}
