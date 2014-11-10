using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Tharsis
{
    [FileExtensions(".mbm", ".txt")]
    public class MBM : BaseFile
    {
        public uint Unknown1 { get; private set; }
        public string MagicNumber { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint Unknown3 { get; private set; }
        public uint NumEntries { get; private set; }
        public uint EntryOffset { get; private set; }

        public List<Entry> Entries { get; private set; }

        public MBM(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            Unknown1 = reader.ReadUInt32();
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            NumEntries = reader.ReadUInt32();
            EntryOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(EntryOffset, SeekOrigin.Begin);

            Entries = new List<Entry>();
            while (Entries.Count < NumEntries) Entries.Add(new Entry(reader));
        }

        public override bool Save(string path)
        {
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                string fileString = string.Format("File: {0}", FilePath.Replace(Program.InputPath, ""));
                writer.WriteLine(fileString.StyleLine(LineType.Underline));
                writer.WriteLine();
                writer.WriteLine("Unknown 1: 0x{0:X8}", Unknown1);
                writer.WriteLine("Magic number: {0}", MagicNumber);
                writer.WriteLine("Unknown 2: 0x{0:X8}", Unknown2);
                writer.WriteLine("Unknown 3: 0x{0:X8}", Unknown3);
                writer.WriteLine("Number of entries: {0}", NumEntries);
                writer.WriteLine("Entry offset: 0x{0:X8}", EntryOffset);
                writer.WriteLine();

                foreach (Entry entry in Entries)
                {
                    writer.WriteLine("Entry offset: 0x{0:X8}", entry.EntryOffset);
                    if (entry.NumBytes == 0 && entry.StringOffset == 0)
                    {
                        writer.WriteLine("(Null entry)");
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine("ID: 0x{0:X8}", entry.ID);
                        writer.WriteLine("Length (bytes): 0x{0:X8}", entry.NumBytes);
                        writer.WriteLine("String offset: 0x{0:X8}", entry.StringOffset);
                        writer.WriteLine("Padding: 0x{0:X8}", entry.Padding);
                        writer.WriteLine("String:");
                        writer.WriteLine("".PadRight(20, '-'));
                        writer.WriteLine(entry.String);
                        writer.WriteLine("".PadRight(20, '-'));
                        writer.WriteLine();
                    }
                }
            }

            return true;
        }

        [DebuggerDisplay("{String}")]
        public class Entry
        {
            public long EntryOffset { get; private set; }

            public uint ID { get; private set; }
            public uint NumBytes { get; private set; }
            public uint StringOffset { get; private set; }
            public uint Padding { get; private set; }

            public string String { get; private set; }

            public Entry(BinaryReader reader)
            {
                EntryOffset = reader.BaseStream.Position;

                ID = reader.ReadUInt32();
                NumBytes = reader.ReadUInt32();
                StringOffset = reader.ReadUInt32();
                Padding = reader.ReadUInt32();

                long streamPosition = reader.BaseStream.Position;
                reader.BaseStream.Seek(StringOffset, SeekOrigin.Begin);

                String = EO4String.GetInstance().GetString(reader.ReadBytes((int)NumBytes));

                reader.BaseStream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }
    }
}
