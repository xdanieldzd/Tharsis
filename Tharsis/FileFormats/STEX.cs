using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

using Tharsis.IO;
using Tharsis.Images;

namespace Tharsis.FileFormats
{
    [FileExtensions(".stex", ".png")]
    public class STEX : BaseFile
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; } /* TODO: sometimes one, mostly zero */
        public uint Constant3553 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public Pica.DataTypes DataType { get; private set; }
        public Pica.PixelFormats PixelFormat { get; private set; }
        public uint NumImageBytes { get; private set; }

        /* Only in "xx80" STEXs */
        public uint ImageOffset { get; private set; }
        public uint Unknown0x24 { get; private set; }
        public string Name { get; private set; }

        public byte[] RawPixelData { get; private set; }

        public Bitmap Image { get; private set; }

        public STEX(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            // "E:\[SSD User Data]\Downloads\EOIV\romfs" --ascii --colorindex --keep --output "E:\[SSD User Data]\Downloads\EOIV\dump"

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown0x04 = reader.ReadUInt32();
            Constant3553 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            DataType = (Pica.DataTypes)reader.ReadUInt32();
            PixelFormat = (Pica.PixelFormats)reader.ReadUInt32();
            NumImageBytes = reader.ReadUInt32();

            if ((reader.BaseStream.Length & 0x000000FF) == 0x00 || (reader.BaseStream.Length & 0x000000FF) == 0x80)
            {
                ImageOffset = reader.ReadUInt32();
                Unknown0x24 = reader.ReadUInt32();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(0x58), 0, 0x58).TrimEnd('\0');

                reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);
                RawPixelData = reader.ReadBytes((int)(NumImageBytes > reader.BaseStream.Length ? reader.BaseStream.Length - ImageOffset : NumImageBytes));
            }
            else if ((reader.BaseStream.Length & 0x000000FF) == 0x20)
            {
                ImageOffset = (uint)reader.BaseStream.Position;
                Unknown0x24 = uint.MaxValue;
                Name = string.Empty;

                RawPixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }
            else
                throw new NotImplementedException("Malformed or unrecognized STEX header; please report");

            Image = Texture.ToBitmap(DataType, PixelFormat, (int)Width, (int)Height, RawPixelData);
        }

        public override bool Save(string path)
        {
            if (Image != null)
            {
                Image.Save(path);
                return true;
            }
            else
                return false;
        }
    }
}
