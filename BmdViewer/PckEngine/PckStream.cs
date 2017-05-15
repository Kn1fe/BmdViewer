using ComponentAce.Compression.Libs.zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace BmdViewer.PckEngine
{
    public class fileTableEntry
    {
        public string filePath { get; set; }
        public string fullFilePath { get; set; }
        public uint fileDataOffset { get; set; }
        public int fileDataDecompressedSize { get; set; }
        public int fileDataCompressedSize { get; set; }
    }

    public class PckStream
    {
        public event DSetProgressValue SetProgressValue;
        public event DSetProgressMaximum SetProgressMaximum;

        private string pck_path = "";
        private List<fileTableEntry> files = new List<fileTableEntry>();

        int KEY_1 = -1466731422;
        int KEY_2 = -240896429;

        public void Load(string path)
        {
            new Thread(delegate()
            {
                pck_path = path;
                PckReader br = new PckReader(path);
                br.Seek(-8, SeekOrigin.End);
                int entryCount = br.ReadInt32();
                br.Seek(-272, SeekOrigin.End);
                long fileTableOffset = (long)((ulong)((uint)(br.ReadUInt32() ^ (ulong)KEY_1)));
                br.Seek(fileTableOffset, SeekOrigin.Begin);
                SetProgressMaximum(entryCount);
                for (int a = 0; a < entryCount; ++a)
                {
                    SetProgressValue(a);
                    int entrySize = br.ReadInt32() ^ KEY_1;
                    br.ReadInt32();
                    byte[] buffer = br.ReadBytes(entrySize);
                    files.Add(entrySize < 276 ? readTableEntry(buffer, true) : readTableEntry(buffer, false));
                }
                SetProgressValue(0);
                br.Close();
            }).Start();
        }

        public byte[] ReadFile(string path)
        {
            var file = files.Where(x => x.filePath.ToLower().Contains(path.ToLower())).ToList();
            MemoryStream ms = new MemoryStream();
            if (file.Count > 0)
            {
                fileTableEntry f = file.First();
                PckReader br = new PckReader(pck_path);
                br.Seek(f.fileDataOffset, SeekOrigin.Begin);
                byte[] buffer = br.ReadBytes(f.fileDataCompressedSize);
                if (f.fileDataCompressedSize < f.fileDataDecompressedSize)
                {
                    ZOutputStream zos = new ZOutputStream(ms);
                    CopyStream(new MemoryStream(buffer), zos, f.fileDataCompressedSize);
                }
                else
                {
                    return buffer;
                }
            }
            return ms.ToArray();
        }

        public fileTableEntry readTableEntry(byte[] buffer, bool compressed)
        {
            fileTableEntry fte = new fileTableEntry();
            if (compressed)
            {
                byte[] buf = new byte[276];
                ZOutputStream zos = new ZOutputStream(new MemoryStream(buf));
                CopyStream(new MemoryStream(buffer), zos, 276);
                buffer = buf;
            }
            BinaryReader br = new BinaryReader(new MemoryStream(buffer));
            fte.filePath = Encoding.GetEncoding("GB2312").GetString(br.ReadBytes(260)).Split(new string[] { "\0" }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("/", "\\");
            fte.fullFilePath = string.Empty;
            fte.fileDataOffset = br.ReadUInt32();
            fte.fileDataDecompressedSize = br.ReadInt32();
            fte.fileDataCompressedSize = br.ReadInt32();
            return fte;
        }

        public void CopyStream(Stream input, Stream output, int Size)
        {
            try
            {
                byte[] buffer = new byte[Size];
                int len;
                while ((len = input.Read(buffer, 0, Size)) > 0)
                {
                    output.Write(buffer, 0, len);
                }
                output.Flush();
            }
            catch
            {
                Console.WriteLine("\nBad zlib data");
            }
        }
    }
}
