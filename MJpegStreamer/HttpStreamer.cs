using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MJpegStreamer
{
    public class HttpStreamer
    {
        private Boolean isShutdown = false;
        private Object thisLock = new Object();
        private MultipartHelper helper = new MultipartHelper("END", "image/jpeg");
        private TcpListener listener;
        private Thread processingThread;

        private List<SocketHolder> connections = new List<SocketHolder>();

        private ConcurrentQueue<Image> queue;

        public HttpStreamer(int port, ConcurrentQueue<Image> queue) {
            this.queue = queue;
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.BeginAcceptSocket(new AsyncCallback(AddSocket), listener);
            processingThread = new Thread(new ThreadStart(this.InternalLoop));
            processingThread.Start();
        }

        private void AddSocket(IAsyncResult ar)
        {
            Console.WriteLine("Connection in");
            lock (thisLock)
            {
                if (isShutdown)
                {
                    return;
                }
                TcpListener listener = (TcpListener)ar.AsyncState;
                Socket s = listener.EndAcceptSocket(ar);
                Console.WriteLine(s);
                SocketHolder holder = new SocketHolder(s, new NetworkStream(s));
                helper.WriteHeader(holder);
                connections.Add(holder);
            }
        }

        public void Shutdown()
        {
            lock (thisLock)
            {
                if (isShutdown)
                {
                    return;
                }

                isShutdown = true;

                listener.Stop();

                foreach (SocketHolder holder in connections)
                {
                    helper.WriteFinalBoundary(holder);
                    holder.Close();
                }
            }
        }

        private void InternalLoop()
        {
            Console.WriteLine("Entered thread loop");
            Image img;
            SocketHolder holder;
            while (true)
            {
                lock (thisLock)
                {
                    if (isShutdown)
                    {
                        break;
                    }

                    if (queue.TryDequeue(out img))
                    {
                        for (int index = connections.Count - 1; index >= 0; index--) {
                            holder = connections[index];
                            try
                            {
                                helper.WriteBoundary(holder);
                                img.Save(holder.Stream, ImageFormat.Jpeg);
                            }
                            catch (IOException e)
                            {
                                Console.WriteLine(e);
                                connections.RemoveAt(index);
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Exited thread loop");

            Shutdown();
        }
    }
}
