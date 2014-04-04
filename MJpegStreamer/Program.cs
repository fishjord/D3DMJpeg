using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        }
    }
}
