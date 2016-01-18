using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

using Tharsis.Images;

namespace Tharsis
{
    [FileExtensions(".stex", ".png")]
    public class STEX : BaseFile
    {
        public string MagicNumber { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public Pica.DataTypes DataType { get; private set; }
        public Pica.PixelFormats PixelFormat { get; private set; }
        public uint NumImageBytes { get; private set; }
        public uint ImageOffset { get; private set; }

        public Bitmap Image { get; private set; }

        public STEX(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            // "E:\[SSD User Data]\Downloads\EOIV\romfs" --ascii --colorindex --keep --output "E:\[SSD User Data]\Downloads\EOIV\dump"

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            DataType = (Pica.DataTypes)reader.ReadUInt32();
            PixelFormat = (Pica.PixelFormats)reader.ReadUInt32();
            NumImageBytes = reader.ReadUInt32();
            ImageOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(0x28, SeekOrigin.Begin);
            if (reader.ReadUInt32() != 0 || reader.ReadUInt32() != 0) ImageOffset = 0x20;

            reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);

            Image = Texture.ToBitmap(DataType, PixelFormat, (int)Width, (int)Height, reader);
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
