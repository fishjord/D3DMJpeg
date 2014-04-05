using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MJpegStreamer
{
    public class SocketHolder
    {
        private Socket sock;
        private Stream stream;

        public Stream Stream
        {
            get
            {
                return stream;
            }
        }

        public SocketHolder(Socket sock, Stream stream)
        {
            this.sock = sock;
            this.stream = stream;
        }

        public void Write(byte[] b)
        {
            stream.Write(b, 0, b.Length);
        }

        public void Close()
        {
            stream.Flush();
            stream.Close();
            sock.Close();
        }
    }

    public class MultipartHelper
    {
        private string boundary;
        private string contentType;
        private byte[] boundaryBytes;
        private byte[] finalBoundary;

        public MultipartHelper(string boundary, string contentType)
        {
            this.boundary = boundary;
            boundaryBytes = Encoding.ASCII.GetBytes("\n--" + boundary + "\nContent-type: " + contentType + "\n\n");
            finalBoundary = Encoding.ASCII.GetBytes("\n--" + boundary + "--\n");
        }

        public void WriteHeader(SocketHolder holder)
        {
            holder.Write(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\nServer: fishjord-streaming-frames\nContent-Type: multipart/x-mixed-replace;boundary="
                + boundary + "\nConnection: close\nDate: " + DateTime.Now + "\nStatus: 200\n"));
        }

        public void WriteBoundary(SocketHolder holder)
        {
            holder.Write(boundaryBytes);
        }

        public void WriteFinalBoundary(SocketHolder holder)
        {
            holder.Write(finalBoundary);
        }
    }
}
