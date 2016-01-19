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
        public uint ConstantZero { get; private set; }
        public uint Constant3553 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public Pica.DataTypes DataType { get; private set; }
        public Pica.PixelFormats PixelFormat { get; private set; }
        public uint NumImageBytes { get; private set; }
        public uint ImageOffset { get; private set; }

        public Bitmap Image { get; private set; }
        public byte[] PixelData { get; private set; }

        public STEX(string path, ParseModes mode) : base(path, mode) { }

        protected override void Import(Stream sourceStream)
        {
            // "E:\[SSD User Data]\Downloads\EOIV\romfs" --ascii --colorindex --keep --output "E:\[SSD User Data]\Downloads\EOIV\dump"

            BinaryReader reader = new BinaryReader(sourceStream);

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            ConstantZero = reader.ReadUInt32();
            Constant3553 = reader.ReadUInt32();
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

            reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)NumImageBytes);
        }

        protected override void Export(Stream sourceStream)
        {
            using (Bitmap tempBitmap = new Bitmap(sourceStream))
            {
                Image = new Bitmap(tempBitmap.Width, tempBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics gr = Graphics.FromImage(Image))
                {
                    gr.DrawImage(tempBitmap, new Rectangle(0, 0, Image.Width, Image.Height));
                }
                PixelData = Texture.ToBytes(Image, Program.ImageOutputDataType, Program.ImageOutputPixelFormat);
            }
        }

        public override bool Save(string path)
        {
            switch (this.ParseMode)
            {
                case ParseModes.ImportFormat:
                    if (Image != null)
                    {
                        Image.Save(path);
                        return true;
                    }
                    break;

                case ParseModes.ExportFormat:
                    using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        BinaryWriter writer = new BinaryWriter(stream);

                        writer.Write(Encoding.ASCII.GetBytes("STEX"));
                        writer.Write((uint)0);
                        writer.Write((uint)3553);
                        writer.Write((uint)Image.Width);
                        writer.Write((uint)Image.Height);
                        writer.Write((uint)Program.ImageOutputDataType);
                        writer.Write((uint)Program.ImageOutputPixelFormat);
                        long numImageBytesPosition = writer.BaseStream.Position;
                        writer.Write(uint.MaxValue);
                        writer.Write((uint)0x80);
                        while (writer.BaseStream.Position < 0x80) writer.Write((byte)0);

                        writer.Write(PixelData);

                        writer.BaseStream.Seek(numImageBytesPosition, SeekOrigin.Begin);
                        writer.Write((uint)(writer.BaseStream.Length - 0x80));
                    }
                    return true;
            }

            return false;
        }
    }
}
