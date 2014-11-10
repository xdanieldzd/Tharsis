using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Tharsis
{
    [FileExtensions(".stex", ".png")]
    public class STEX : BaseFile
    {
        [DllImport("ETC1Lib.dll", EntryPoint = "ConvertETC1", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ConvertETC1(IntPtr dataOut, ref UInt32 dataOutSize, IntPtr dataIn, UInt16 width, UInt16 height, bool alpha);

        public enum Formats : uint
        {
            RGBA8 = 0x14016752,
            RGBA4 = 0x80336752,
            RGBA5551 = 0x80346752,
            RGB565 = 0x83636754,
            L8 = 0x14016756,
            A8 = 0x14016757,
            LA4 = 0x67616757,
            LA8 = 0x14016758,
            ETC1 = 0x1401675A,
            ETC1A4 = 0x1401675B
        };

        Dictionary<Formats, byte> bytesPerPixel = new Dictionary<Formats, byte>()
        {
            { Formats.RGBA8, 4 },
            { Formats.RGBA5551, 2 },
            { Formats.RGB565, 2 },
            { Formats.ETC1, 4 },
            { Formats.ETC1A4, 4 }
        };

        readonly int[] Convert5To8 =
        {
            0x00,0x08,0x10,0x18,0x20,0x29,0x31,0x39,
            0x41,0x4A,0x52,0x5A,0x62,0x6A,0x73,0x7B,
            0x83,0x8B,0x94,0x9C,0xA4,0xAC,0xB4,0xBD,
            0xC5,0xCD,0xD5,0xDE,0xE6,0xEE,0xF6,0xFF
        };

        public string MagicNumber { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint Format1 { get; private set; }
        public uint Format2 { get; private set; }
        public uint NumImageBytes { get; private set; }
        public uint ImageOffset { get; private set; }

        public Formats Format { get; private set; }
        public Bitmap Image { get; private set; }

        public STEX(string path) : base(path) { }

        protected override void Parse(BinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Format1 = reader.ReadUInt32();
            Format2 = reader.ReadUInt32();
            NumImageBytes = reader.ReadUInt32();
            ImageOffset = reader.ReadUInt32();

            Format = (Formats)(((Format1 & 0xFFFF) << 16) | Format2 & 0xFFFF);

            if (!bytesPerPixel.ContainsKey(Format)) return;

            reader.BaseStream.Seek(0x28, SeekOrigin.Begin);
            if (reader.ReadUInt32() != 0 || reader.ReadUInt32() != 0) ImageOffset = 0x20;

            reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);

            Image = new Bitmap((int)Width, (int)Height);
            if (Format == Formats.ETC1 || Format == Formats.ETC1A4)
            {
                try
                {
                    /* Get compressed data & handle to it */
                    byte[] textureData = reader.ReadBytes((int)NumImageBytes);
                    ushort[] input = new ushort[textureData.Length / sizeof(ushort)];
                    Buffer.BlockCopy(textureData, 0, input, 0, textureData.Length);
                    GCHandle pInput = GCHandle.Alloc(input, GCHandleType.Pinned);

                    /* Marshal data around, invoke ETC1Lib.dll for conversion, etc */
                    UInt32 dataSize = 0;
                    UInt16 marshalWidth = (ushort)Width, marshalHeight = (ushort)Height;
                    ConvertETC1(IntPtr.Zero, ref dataSize, IntPtr.Zero, marshalWidth, marshalHeight, (Format == Formats.ETC1A4));
                    uint[] output = new uint[dataSize];
                    GCHandle pOutput = GCHandle.Alloc(output, GCHandleType.Pinned);
                    ConvertETC1(pOutput.AddrOfPinnedObject(), ref dataSize, pInput.AddrOfPinnedObject(), marshalWidth, marshalHeight, (Format == Formats.ETC1A4));
                    pOutput.Free();
                    pInput.Free();

                    /* Unscramble if needed // could probably be done in ETC1Lib.dll, it's probably pretty damn ugly, but whatever... */
                    /* Non-square code blocks could need some cleanup, verification, etc. as well... */
                    uint[] finalized = new uint[output.Length];

                    if (marshalWidth == marshalHeight)
                    {
                        /* Perfect square, just copy over */
                        Buffer.BlockCopy(output, 0, finalized, 0, finalized.Length);
                    }
                    else if (marshalWidth > marshalHeight)
                    {
                        /* Wider than tall */
                        int numBlocks = (Math.Max(marshalWidth, marshalHeight) / Math.Min(marshalWidth, marshalHeight));
                        int rowNumBytes = (marshalWidth << 5);
                        int blockNumBytes = (rowNumBytes / numBlocks);
                        int lineNumBytes = (blockNumBytes / 8);
                        int source = 0, target = 0;

                        for (int y = 0; y < marshalHeight / 8; y++)
                        {
                            for (int b = 0; b < numBlocks; b++)
                            {
                                for (int y2 = 0; y2 < 8; y2++)
                                {
                                    source = (y * rowNumBytes) + (b * blockNumBytes) + (y2 * lineNumBytes);
                                    target = (y * rowNumBytes) + (y2 * lineNumBytes * numBlocks) + (b * lineNumBytes);
                                    Buffer.BlockCopy(output, source, finalized, target, lineNumBytes);
                                }
                            }
                        }
                    }
                    else
                    {
                        /* Taller than wide */
                        int factor = (marshalHeight / marshalWidth);
                        int lineSize = (marshalWidth * 4);
                        int readOffset = 0, writeOffset = 0;

                        while (readOffset < output.Length)
                        {
                            for (int t = 0; t < 8; t++)
                            {
                                for (int i = 0; i < factor; i++)
                                {
                                    Buffer.BlockCopy(output, readOffset, finalized, writeOffset + (lineSize * 8 * i) + (t * lineSize), lineSize);
                                    readOffset += lineSize;
                                }
                            }
                            writeOffset += (lineSize * factor * 8);
                        }
                    }

                    /* Finally create texture bitmap from decompressed/unscrambled data */
                    byte[] tmp = new byte[finalized.Length];
                    Buffer.BlockCopy(finalized, 0, tmp, 0, tmp.Length);

                    int k = 0;
                    for (int y = 0; y < marshalHeight; y++)
                    {
                        for (int x = 0; x < marshalWidth; x++)
                        {
                            Image.SetPixel(x, y, Color.FromArgb(tmp[k + 3], tmp[k + 0], tmp[k + 1], tmp[k + 2]));
                            k += bytesPerPixel[Format];
                        }
                    }
                }
                catch (System.IndexOutOfRangeException)
                {
                    //
                }
                catch (System.AccessViolationException)
                {
                    //
                }
            }
            else
            {
                for (int y = 0; y < Height; y += 8)
                {
                    for (int x = 0; x < Width; x += 8)
                    {
                        DecodeTile(8, 8, x, y, Image, reader, Format);
                    }
                }
            }
        }

        private Color DecodeColor(int val, Formats format)
        {
            int alpha = 255, red = 255, green = 255, blue = 255;

            switch (format)
            {
                case Formats.RGBA8:
                    red = ((val >> 24) & 0xFF);
                    green = ((val >> 16) & 0xFF);
                    blue = ((val >> 8) & 0xFF);
                    alpha = (val & 0xFF);
                    break;

                case Formats.RGBA5551:
                    red = Convert5To8[(val >> 11) & 0x1F];
                    green = Convert5To8[(val >> 6) & 0x1F];
                    blue = Convert5To8[(val >> 1) & 0x1F];
                    alpha = (val & 0x0001) == 1 ? 0xFF : 0x00;
                    break;

                case Formats.RGB565:
                    red = Convert5To8[(val >> 11) & 0x1F];
                    green = ((val >> 5) & 0x3F) * 4;
                    blue = Convert5To8[val & 0x1F];
                    break;
            }

            return Color.FromArgb(alpha, red, green, blue);
        }

        private void DecodeTile(int iconSize, int tileSize, int ax, int ay, Bitmap image, BinaryReader reader, Formats format)
        {
            if (tileSize == 0)
            {
                byte[] bytes = new byte[4];
                Buffer.BlockCopy(reader.ReadBytes(bytesPerPixel[format]), 0, bytes, 0, bytesPerPixel[format]);
                image.SetPixel(ax, ay, DecodeColor(BitConverter.ToInt32(bytes, 0), format));
            }
            else
                for (var y = 0; y < iconSize; y += tileSize)
                    for (var x = 0; x < iconSize; x += tileSize)
                        DecodeTile(tileSize, tileSize / 2, x + ax, y + ay, image, reader, format);
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
