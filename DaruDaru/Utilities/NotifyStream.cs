using System;
using System.IO;

namespace DaruDaru.Utilities
{
    internal class StreamWithNotify : Stream
    {
        private readonly Stream m_baseStream;
        private readonly Action<int> m_notify;

        public StreamWithNotify(Stream baseStream, Action<int> notify)
        {
            this.m_baseStream = baseStream;
            this.m_notify = notify;
        }

        public override bool CanRead
            => this.m_baseStream.CanRead;

        public override bool CanSeek
            => this.m_baseStream.CanSeek;

        public override bool CanWrite
            => this.m_baseStream.CanWrite;

        public override long Length
            => this.m_baseStream.Length;

        public override long Position
        {
            get => this.m_baseStream.Position;
            set => this.m_baseStream.Position = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.m_notify(count);
            this.m_baseStream.Write(buffer, offset, count);
        }

        public override void Flush()
            => this.m_baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
            => this.m_baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => this.m_baseStream.Seek(offset, origin);

        public override void SetLength(long value)
            => this.m_baseStream.SetLength(value);
    }
}
