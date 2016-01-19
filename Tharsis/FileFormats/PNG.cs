using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Tharsis.IO;
using Tharsis.Images;

namespace Tharsis.FileFormats
{
    [FileExtensions(".png", null)]
    public class PNG : BaseFile
    {
        public Bitmap Image { get; private set; }

        public byte[] PixelData { get; private set; }

        public PNG(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            using (Bitmap tempBitmap = new Bitmap(reader.BaseStream))
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
            FileExtensionsAttribute fileExtAttrib = (Program.ImageOutputType.GetCustomAttributes(typeof(FileExtensionsAttribute), false)[0] as FileExtensionsAttribute);
            string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + fileExtAttrib.SourceExtension);

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(32)))
            {
                if (Program.ImageOutputType == typeof(STEX))
                {
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

                    writer.BaseStream.Seek(0, SeekOrigin.Begin);
                }
                else
                    return false;

                using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    writer.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.BaseStream.CopyTo(stream);
                }
                return true;
            }
        }
    }
}
