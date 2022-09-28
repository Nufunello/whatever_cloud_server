using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace storage
{
    public class NonDisposableStream
        : System.IO.Stream
    {
        Stream stream;
        bool isClosed;

        public NonDisposableStream(Stream stream)
        {
            this.stream = stream;
            this.stream.Position = 0;
            this.isClosed = false;
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Position = 0;
        }


        public void RealClose()
        {
            if (!isClosed)
            {
                using (var d = stream)
                {
                    stream = null;
                }
                GC.Collect();
            }
        }

        ~NonDisposableStream()
        {
            RealClose();
        }
    }
}
