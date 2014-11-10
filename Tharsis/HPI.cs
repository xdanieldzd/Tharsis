using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Tharsis
{
    [FileExtensions(".hpi", null)]
    public class HPI : BaseFile
    {
        public string MagicNumber { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint Unknown3 { get; private set; }
        public ushort Unknown4 { get; private set; }
        public ushort NumUnknownEntries { get; private set; }
        public ushort NumFileEntries { get; private set; }
        public ushort Unknown5 { get; private set; }

        public List<UnknownEntry> UnknownEntries { get; private set; }
        public List<FileEntry> FileEntries { get; private set; }

        public byte[] FilenameTable { get; private set; }

        public string HPBPath { get; private set; }

        public HPI(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            Unknown4 = reader.ReadUInt16();
            NumUnknownEntries = reader.ReadUInt16();
            NumFileEntries = reader.ReadUInt16();
            Unknown5 = reader.ReadUInt16();

            UnknownEntries = new List<UnknownEntry>();
            while (UnknownEntries.Count < NumUnknownEntries) UnknownEntries.Add(new UnknownEntry(reader));

            FileEntries = new List<FileEntry>();
            while (FileEntries.Count < NumFileEntries) FileEntries.Add(new FileEntry(reader));

            FilenameTable = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

            HPBPath = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath) + ".hpb");
            if (!File.Exists(HPBPath)) throw new FileNotFoundException(string.Format("HPB binary for {0} not found", Path.GetFileName(FilePath)));
        }

        private string GetString(BinaryReader reader, Encoding encoding)
        {
            List<byte> bytes = new List<byte>();
            while (bytes.Count == 0 || bytes.Last() != 0) bytes.Add(reader.ReadByte());
            return Encoding.GetEncoding(932).GetString(bytes.ToArray()).TrimEnd('\0');
        }

        public override bool Save(string path)
        {
            string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            Directory.CreateDirectory(outputPath);

            using (BinaryReader hpbReader = new BinaryReader(File.Open(HPBPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                using (BinaryReader filenameReader = new BinaryReader(new MemoryStream(FilenameTable)))
                {
                    for (int i = 0; i < NumFileEntries; i++)
                    {
                        filenameReader.BaseStream.Seek(FileEntries[i].FilenameOffset, SeekOrigin.Begin);
                        string filePath = GetString(filenameReader, Encoding.GetEncoding(932));

                        if (FileEntries[i].FileOffset >= hpbReader.BaseStream.Length || FileEntries[i].FileSize == 0)
                        {
                            Console.WriteLine(" ... cannot process {0}", filePath);
                            continue;
                        }

                        string fullPath = Path.Combine(outputPath, filePath);
                        hpbReader.BaseStream.Seek(FileEntries[i].FileOffset, SeekOrigin.Begin);

                        if (!Directory.Exists(Path.GetDirectoryName(fullPath))) Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        File.WriteAllBytes(fullPath, hpbReader.ReadBytes((int)FileEntries[i].FileSize));
                    }
                }
            }

            return true;
        }

        public class UnknownEntry
        {
            public long EntryOffset { get; private set; }

            public ushort Unknown1 { get; private set; }
            public ushort Unknown2 { get; private set; }

            public UnknownEntry(BinaryReader reader)
            {
                EntryOffset = reader.BaseStream.Position;

                Unknown1 = reader.ReadUInt16();
                Unknown2 = reader.ReadUInt16();
            }
        }

        public class FileEntry
        {
            public long EntryOffset { get; private set; }

            public uint FilenameOffset { get; private set; }
            public uint FileOffset { get; private set; }
            public uint FileSize { get; private set; }
            public uint Unknown2 { get; private set; }

            public FileEntry(BinaryReader reader)
            {
                EntryOffset = reader.BaseStream.Position;

                FilenameOffset = reader.ReadUInt32();
                FileOffset = reader.ReadUInt32();
                FileSize = reader.ReadUInt32();
                Unknown2 = reader.ReadUInt32();
            }
        }
    }
}
