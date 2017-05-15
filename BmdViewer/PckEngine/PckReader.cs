using System;
using System.IO;

namespace BmdViewer.PckEngine
{
    internal class PckReader : FileStream
    {
        protected FileStream pkx_fs = null;
        private new long Position = 0;

        public PckReader(string path) : base(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1200000)
        {
            if (File.Exists(path.Replace(".pck", ".pkx"))) pkx_fs = new FileStream(path.Replace(".pck", ".pkx"), FileMode.Open, FileAccess.Read, FileShare.Read, 1200000);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long len = GetLenght();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = len + offset;
                    break;
            }
            return Position;
        }

        private long GetLenght()
        {
            return pkx_fs != null ? Length + pkx_fs.Length : Length;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] array = new byte[count];
            int BytesRead = 0;
            if (Position < Length)
            {
                base.Seek(Position, SeekOrigin.Begin);
                BytesRead = Read(array, 0, count);
                if (BytesRead < count && pkx_fs != null)
                {
                    pkx_fs.Seek(0, SeekOrigin.Begin);
                    BytesRead += pkx_fs.Read(array, BytesRead, count - BytesRead);
                }
            }
            else if (Position > Length && pkx_fs != null)
            {
                pkx_fs.Seek(Position - Length, SeekOrigin.Begin);
                BytesRead = pkx_fs.Read(array, 0, count);
            }
            Position += count;
            return array;
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4), 0);
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }
    }
}