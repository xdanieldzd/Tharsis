﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Tharsis.IO;

namespace Tharsis.FileFormats
{
    /* TODO: rewrite to be clearner, w/ less duplication! Maybe generalize TileCodecs code to allow non-tiled images to be converted? Take code from GXTConvert for that? */

    /* Better palette conversion based on information from... */
    /* http://forums.pcsx2.net/Thread-TMX-file-format-in-Persona-3-4 */
    /* http://forum.xentax.com/viewtopic.php?f=18&t=2922&start=0 */

    [Flags]
    public enum TMXWrapMode
    {
        HorizontalRepeat = 0x0000,
        VerticalRepeat = 0x0000,
        HorizontalClamp = 0x0100,
        VerticalClamp = 0x0400,
    }

    public enum TMXPixelFormat
    {
        PSMCT32 = 0x00,
        PSMCT24 = 0x01,
        PSMCT16 = 0x02,
        PSMCT16S = 0x0A,
        PSMT8 = 0x13,
        PSMT4 = 0x14,
        PSMT8H = 0x1B,
        PSMT4HL = 0x24,
        PSMT4HH = 0x2C
    }

    [FileExtensions(".tmx", ".png")]
    public class TMX : BaseFile
    {
        public const string ExpectedMagic = "TMX0";

        public ushort Unknown1 { get; private set; }
        public ushort ID { get; private set; }
        public uint FileSize { get; private set; }
        public string MagicNumber { get; private set; }
        public uint Unknown2 { get; private set; }
        public byte Unknown3 { get; private set; }
        public TMXPixelFormat PaletteFormat { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public TMXPixelFormat PixelFormat { get; private set; }
        public byte MipmapCount { get; private set; }
        public byte MipmapKValue { get; private set; }
        public byte MipmapLValue { get; private set; }
        public ushort Unknown4 { get; private set; }
        public TMXWrapMode TextureWrap { get; private set; }
        public uint TextureID { get; private set; }
        public uint CLUTID { get; private set; }
        public string Comment { get; private set; }
        public Color[] Palette { get; private set; }
        public byte[] PixelData { get; private set; }

        public Bitmap Image { get; private set; }

        BitmapData bmpData;
        byte[] pixelData;

        public TMX(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            /* Check for TMX0 magic word; try searching for it if not found at expected address */
            reader.BaseStream.Seek(0x08, SeekOrigin.Begin);
            if (Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4) != ExpectedMagic)
            {
                long result = reader.FindString(ExpectedMagic);
                if (result != -1)
                    reader.BaseStream.Seek(result - 8, SeekOrigin.Begin);
                else
                    throw new FileFormatException("File could not be recognized as TMX");
            }
            else
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

            /* Read TMX0 header */
            Unknown1 = reader.ReadUInt16();
            ID = reader.ReadUInt16();
            FileSize = reader.ReadUInt32();
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadByte();
            PaletteFormat = (TMXPixelFormat)reader.ReadByte();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PixelFormat = (TMXPixelFormat)reader.ReadByte();
            MipmapCount = reader.ReadByte();
            MipmapKValue = reader.ReadByte();
            MipmapLValue = reader.ReadByte();
            TextureWrap = (TMXWrapMode)reader.ReadUInt16();
            TextureID = reader.ReadUInt32();
            CLUTID = reader.ReadUInt32();
            Comment = Encoding.ASCII.GetString(reader.ReadBytes(0x1C), 0, 0x1C);

            /* Convert image according to color depth */
            switch (PixelFormat)
            {
                case TMXPixelFormat.PSMT8: ConvertIndexed8(reader); break;
                case TMXPixelFormat.PSMT4: ConvertIndexed4(reader); break;
                case TMXPixelFormat.PSMCT16: Convert16(reader); break;
                case TMXPixelFormat.PSMCT24: Convert24(reader); break;
                case TMXPixelFormat.PSMCT32: Convert32(reader); break;
                default: throw new FileFormatException(string.Format("Unrecognized pixel format {0}", PixelFormat));
            }
        }

        private byte ScaleAlpha(byte a)
        {
            /* Required for indexed AND non-indexed formats */
            return (byte)Math.Min((255.0f * (a / 128.0f)), 0xFF);
        }

        private Color[] ConvertPalette(BinaryReader reader, int colorCount)
        {
            /* Read palette to temporary array, scale alpha while doing so */
            Color[] tempPalette = new Color[colorCount];
            for (int i = 0; i < tempPalette.Length; i++)
            {
                byte r, g, b, a;
                if (PaletteFormat == TMXPixelFormat.PSMCT32)
                {
                    uint color = reader.ReadUInt32();
                    r = (byte)color;
                    g = (byte)(color >> 8);
                    b = (byte)(color >> 16);
                    a = ScaleAlpha(Program.IgnoreTMXAlpha ? (byte)0x80 : (byte)(color >> 24));
                }
                else
                {
                    ushort color = reader.ReadUInt16();
                    r = (byte)((color & 0x001F) << 3);
                    g = (byte)(((color & 0x03E0) >> 5) << 3);
                    b = (byte)(((color & 0x7C00) >> 10) << 3);
                    a = ScaleAlpha(Program.IgnoreTMXAlpha ? (byte)0x80 : (byte)(i == 0 ? 0 : 0x80));
                }
                tempPalette[i] = Color.FromArgb(a, r, g, b);
            }

            /* Create output array; "descramble" palette if needed (8bpp) */
            Color[] newPalette = new Color[colorCount];
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

        private void SetupBitmap(bool indexed)
        {
            if (indexed)
            {
                /* Copy palette to image */
                ColorPalette imagePalette = Image.Palette;
                Array.Copy(Palette, imagePalette.Entries, Palette.Length);
                Image.Palette = imagePalette;
            }

            /* Lock bitmap, prepare byte array for writing */
            bmpData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, Image.PixelFormat);
            pixelData = new byte[bmpData.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);
        }

        private void FinalizeBitmap()
        {
            /* Copy array back, then unlock bitmap */
            Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
            Image.UnlockBits(bmpData);
        }

        private void ConvertIndexed4(BinaryReader reader)
        {
            Palette = ConvertPalette(reader, 16);

            if (Program.ConvertTMXIndexed)
            {
                Image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format4bppIndexed);

                SetupBitmap(true);

                int dataSize = (Width * Height) / 2;
                Buffer.BlockCopy(reader.ReadBytes(dataSize), 0, pixelData, 0, dataSize);
                /* Swap nibbles */
                for (int i = 0; i < pixelData.Length; i++) pixelData[i] = (byte)((pixelData[i] >> 4) | (pixelData[i] << 4));

                FinalizeBitmap();
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

        private void ConvertIndexed8(BinaryReader reader)
        {
            Palette = ConvertPalette(reader, 256);

            if (Program.ConvertTMXIndexed)
            {
                Image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                SetupBitmap(true);

                int dataSize = (Width * Height);
                Buffer.BlockCopy(reader.ReadBytes(dataSize), 0, pixelData, 0, dataSize);

                FinalizeBitmap();
            }
            else
            {
                Image = new Bitmap(Width, Height);
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        Image.SetPixel(x, y, Palette[reader.ReadByte()]);
            }
        }

        /* Copypasta from TileCodecs.cs; didn't want to make that instance public */

        private static byte ResampleChannel(int value, int sourceBits, int targetBits)
        {
            byte sourceMask = (byte)((1 << sourceBits) - 1);
            byte targetMask = (byte)((1 << targetBits) - 1);
            return (byte)((((value & sourceMask) * targetMask) + (sourceMask >> 1)) / sourceMask);
        }

        /* TODO: bmpData Height vs Stride, verify! */

        private void Convert16(BinaryReader reader)
        {
            Image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            SetupBitmap(false);

            for (int i = 0; i < (bmpData.Width * bmpData.Height) * (Bitmap.GetPixelFormatSize(Image.PixelFormat) / 8); i += 4)
            {
                ushort rgba = reader.ReadUInt16();
                pixelData[i + 2] = ResampleChannel((rgba >> 0), 5, 8);
                pixelData[i + 1] = ResampleChannel((rgba >> 5), 5, 8);
                pixelData[i + 0] = ResampleChannel((rgba >> 10), 5, 8);
                /* TODO: verify alpha */
                pixelData[i + 3] = ScaleAlpha(Program.IgnoreTMXAlpha ? (byte)0x80 : ResampleChannel((rgba >> 15), 1, 8));
            }
            FinalizeBitmap();
        }

        private void Convert24(BinaryReader reader)
        {
            Image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            SetupBitmap(false);

            for (int i = 0; i < (bmpData.Width * bmpData.Height) * (Bitmap.GetPixelFormatSize(Image.PixelFormat) / 8); i += 4)
            {
                pixelData[i + 2] = reader.ReadByte();
                pixelData[i + 1] = reader.ReadByte();
                pixelData[i + 0] = reader.ReadByte();
                pixelData[i + 3] = ScaleAlpha(0x80);
            }
            FinalizeBitmap();
        }

        private void Convert32(BinaryReader reader)
        {
            Image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            SetupBitmap(false);

            for (int i = 0; i < (bmpData.Width * bmpData.Height) * (Bitmap.GetPixelFormatSize(Image.PixelFormat) / 8); i += 4)
            {
                pixelData[i + 2] = reader.ReadByte();
                pixelData[i + 1] = reader.ReadByte();
                pixelData[i + 0] = reader.ReadByte();
                byte a = reader.ReadByte();
                pixelData[i + 3] = ScaleAlpha(Program.IgnoreTMXAlpha ? (byte)0x80 : a);
            }
            FinalizeBitmap();
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
