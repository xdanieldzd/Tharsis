using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Tharsis
{
    /* Better palette conversion based on information from... */
    /* http://forums.pcsx2.net/Thread-TMX-file-format-in-Persona-3-4 */
    /* http://forum.xentax.com/viewtopic.php?f=18&t=2922&start=0 */

    [FileExtensions(".tmx", ".png")]
    public class TMX : BaseFile
    {
        public const string ExpectedMagic = "TMX0";

        public uint Unknown1 { get; private set; }
        public uint FileSize { get; private set; }
        public string MagicNumber { get; private set; }
        public uint Unknown2 { get; private set; }
        public ushort Unknown3 { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort ColorDepth { get; private set; }
        public uint Unknown5 { get; private set; }
        public uint Unknown6 { get; private set; }
        public byte[] Unknown0x20 { get; private set; }
        public Color[] Palette { get; private set; }
        public byte[] PixelData { get; private set; }

        public Bitmap Image { get; private set; }

        BitmapData bmpData;
        byte[] pixelData;

        public TMX(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            reader.BaseStream.Seek(0x08, SeekOrigin.Begin);

            if (Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4) != ExpectedMagic)
            {
                long result = reader.FindString(ExpectedMagic);
                if (result != -1)
                    reader.BaseStream.Seek(result - 8, SeekOrigin.Begin);
                else
                    throw new Exception("File could not be recognized as TMX");
            }
            else
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

            Unknown1 = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            ColorDepth = reader.ReadUInt16();
            Unknown5 = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            Unknown0x20 = reader.ReadBytes(32);

            switch (ColorDepth)
            {
                case 0x13: Convert8bpp(reader); break;
                case 0x14: Convert4bpp(reader); break;
                default: throw new Exception(string.Format("Unrecognized color depth 0x{0:X2}", ColorDepth));
            }
        }

        private Color[] ConvertPalette(BinaryReader reader, int colorCount)
        {
            Color[] newPalette = new Color[colorCount];
            Color[] tempPalette = new Color[colorCount];

            for (int i = 0; i < tempPalette.Length; i++)
            {
                uint color = reader.ReadUInt32();
                byte alpha = (byte)Math.Min((255.0f * ((byte)(color >> 24) / 128.0f)), 0xFF);
                tempPalette[i] = Color.FromArgb(alpha, (byte)(color & 0xFF), (byte)(color >> 8), (byte)(color >> 16));
            }

            if (colorCount == 256)
            {
                for (int i = 0; i < newPalette.Length; i += 32)
                {
                    Array.Copy(tempPalette, i, newPalette, i, 8);
                    Array.Copy(tempPalette, i + 8, newPalette, i + 16, 8);
                    Array.Copy(tempPalette, i + 16, newPalette, i + 8, 8);
                    Array.Copy(tempPalette, i + 24, newPalette, i + 24, 8);
                }
            }
            else
                Array.Copy(tempPalette, newPalette, tempPalette.Length);

            return newPalette;
        }

        private void Convert4bpp(BinaryReader reader)
        {
            Palette = ConvertPalette(reader, 16);

            if (Program.ConvertTMXIndexed)
            {
                Image = new Bitmap(Width, Height, PixelFormat.Format4bppIndexed);

                int dataSize = (Width * Height) / 2;

                ColorPalette imagePalette = Image.Palette;
                Array.Copy(Palette, imagePalette.Entries, Palette.Length);
                Image.Palette = imagePalette;

                bmpData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, Image.PixelFormat);
                pixelData = new byte[bmpData.Height * bmpData.Stride];
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                Buffer.BlockCopy(reader.ReadBytes(dataSize), 0, pixelData, 0, dataSize);
                for (int i = 0; i < pixelData.Length; i++) pixelData[i] = (byte)((pixelData[i] >> 4) | (pixelData[i] << 4));

                Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                Image.UnlockBits(bmpData);
            }
            else
            {
                Image = new Bitmap(Width, Height);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x += 2)
                    {
                        byte pixels = reader.ReadByte();
                        Image.SetPixel(x, y, Palette[pixels & 0x0F]);
                        Image.SetPixel(x + 1, y, Palette[pixels >> 4]);
                    }
                }
            }
        }

        private void Convert8bpp(BinaryReader reader)
        {
            Palette = ConvertPalette(reader, 256);

            if (Program.ConvertTMXIndexed)
            {
                Image = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);

                int dataSize = (Width * Height);

                ColorPalette imagePalette = Image.Palette;
                Array.Copy(Palette, imagePalette.Entries, Palette.Length);
                Image.Palette = imagePalette;

                bmpData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, Image.PixelFormat);
                pixelData = new byte[bmpData.Height * bmpData.Stride];
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                Buffer.BlockCopy(reader.ReadBytes(dataSize), 0, pixelData, 0, dataSize);

                Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                Image.UnlockBits(bmpData);
            }
            else
            {
                Image = new Bitmap(Width, Height);
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        Image.SetPixel(x, y, Palette[reader.ReadByte()]);
            }
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
