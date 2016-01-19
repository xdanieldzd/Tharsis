using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using Tharsis.IO;

namespace Tharsis.FileFormats
{
    [FileExtensions(".mbm", ".txt")]
    public class MBM : BaseFile
    {
        public uint MaybeAlwaysZero { get; private set; }       /* Always zero? */
        public string MagicNumber { get; private set; }
        public uint MaybeAlways65536 { get; private set; }      /* Always 0x00010000? */
        public uint FileSize { get; private set; }              /* NOTE: Not always correct! Not used & messed up during localization? */
        public uint NumEntries { get; private set; }
        public uint EntryOffset { get; private set; }

        public List<Entry> Entries { get; private set; }

        public uint ActualFileSize { get; private set; }

        public MBM(string path, ParseModes mode) : base(path, mode) { }

        protected override void Import(Stream sourceStream)
        {
            BinaryReader reader = new BinaryReader(sourceStream);

            MaybeAlwaysZero = reader.ReadUInt32();
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            MaybeAlways65536 = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            NumEntries = reader.ReadUInt32();
            EntryOffset = reader.ReadUInt32();

            if (MaybeAlwaysZero != 0 || MaybeAlways65536 != 0x00010000) return;

            reader.BaseStream.Seek(EntryOffset, SeekOrigin.Begin);

            int validEntries = 0;
            Entries = new List<Entry>();
            while (validEntries < NumEntries)
            {
                Entry newEntry = new Entry(reader);
                Entries.Add(newEntry);
                if (newEntry.NumBytes != 0) validEntries++;
            }

            ActualFileSize = (uint)reader.BaseStream.Length;
        }

        public override bool Save(string path)
        {
            if (Entries == null) return false;

            using (StreamWriter writer = new StreamWriter(path, false))
            {
                string fileString = string.Format("File: {0}", FilePath.Replace(Program.InputPath, ""));
                writer.WriteLine(fileString.StyleLine(LineType.Underline));
                writer.WriteLine();
                writer.WriteLine("Unknown 1 (always zero?): 0x{0:X8}", MaybeAlwaysZero);
                writer.WriteLine("Magic number: {0}", MagicNumber);
                writer.WriteLine("Unknown 2 (always 0x10000?): 0x{0:X}", MaybeAlways65536);
                writer.WriteLine("File size (bytes): 0x{0:X}", FileSize);
                writer.WriteLine("- Actual size (bytes): 0x{0:X}", ActualFileSize);
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
                        writer.WriteLine("Length (bytes): 0x{0:X}", entry.NumBytes);
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
