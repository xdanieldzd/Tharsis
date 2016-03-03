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
        public uint NumImageBytes { get; private set; } /* Unreliable! */

        /* Only in "xx80" STEXs */
        public uint ImageOffset { get; private set; }
        public uint Unknown0x24 { get; private set; }
        public string Name { get; private set; } /* Only in newer STEX? (ex. SMT4F) */

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

            /* Disclaimer: Hacky as hell! I almost want to hope that Atlus someday leaves their internal STEX creator tool inside one of these games, maybe that'll help with figuring this out <.< */

            /* ...now at offset 0x20, assume here's the pointer to image data */
            ImageOffset = reader.ReadUInt32();

            /* ...now assume said pointer is 0x80 */
            if (ImageOffset == 0x80)
            {
                /* Read "well-formed" STEX (but really, who knows how the header's supposed to be) */
                Unknown0x24 = reader.ReadUInt32();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(0x58), 0, 0x58).TrimEnd('\0');

                /* ...but as image datasize is also unreliable, do some additional sanity checking on that, too! */
                reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);
                RawPixelData = reader.ReadBytes((int)(NumImageBytes > reader.BaseStream.Length ? reader.BaseStream.Length - ImageOffset : NumImageBytes));
            }
            else /* ...otherwise... */
            {
                /* Seek back, then just assume image data starts right here at 0x20, and that the image is as many bytes long as are left in the file */
                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                ImageOffset = (uint)reader.BaseStream.Position;
                Unknown0x24 = uint.MaxValue;
                Name = string.Empty;

                RawPixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }

            /* I hope this covers every STEX out there right now. If not... I'm running out of ideas */

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
