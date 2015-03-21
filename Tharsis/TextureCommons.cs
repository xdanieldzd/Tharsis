using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tharsis
{
    public class TextureCommons
    {
        [DllImport("ETC1Lib.dll", EntryPoint = "ConvertETC1", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ConvertETC1(IntPtr dataOut, ref UInt32 dataOutSize, IntPtr dataIn, UInt16 width, UInt16 height, bool alpha);

        enum DataTypes : ushort
        {
            Byte = 0x1400,
            UnsignedByte = 0x1401,
            Short = 0x1402,
            UnsignedShort = 0x1403,
            Int = 0x1404,
            UnsignedInt = 0x1405,
            Float = 0x1406,
            UnsignedByte44DMP = 0x6760,
            Unsigned4BitsDMP = 0x6761,
            UnsignedShort4444 = 0x8033,
            UnsignedShort5551 = 0x8034,
            UnsignedShort565 = 0x8363
        };

        enum TextureFormats : ushort
        {
            RGBANativeDMP = 0x6752,
            RGBNativeDMP = 0x6754,
            AlphaNativeDMP = 0x6756,
            LuminanceNativeDMP = 0x6757,
            LuminanceAlphaNativeDMP = 0x6758,
            ETC1RGB8NativeDMP = 0x675A,
            ETC1AlphaRGB8A4NativeDMP = 0x675B
        };

        public enum Formats
        {
            ETC1,
            ETC1A4,
            RGBA8888,
            RGB888,
            RGBA4444,
            RGBA5551,
            RGB565,
            LA44,
            LA88,
            L8,
            A8,
            L4,
            A4
        };

        public static Dictionary<uint, Formats> FormatMap = new Dictionary<uint, Formats>()
        {
            /* Atlus STEX */
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.ETC1RGB8NativeDMP), Formats.ETC1 },
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.ETC1AlphaRGB8A4NativeDMP), Formats.ETC1A4 },

            /* OoT/MM3D CMB/CTXB */
            { (uint)TextureFormats.ETC1RGB8NativeDMP, Formats.ETC1 },
            { (uint)TextureFormats.ETC1AlphaRGB8A4NativeDMP, Formats.ETC1A4 },

            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.RGBANativeDMP), Formats.RGBA8888 },
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.RGBNativeDMP), Formats.RGB888 },
            { ((uint)DataTypes.UnsignedShort4444 << 16 | (uint)TextureFormats.RGBANativeDMP), Formats.RGBA4444 },
            { ((uint)DataTypes.UnsignedShort5551 << 16 | (uint)TextureFormats.RGBANativeDMP), Formats.RGBA5551 },
            { ((uint)DataTypes.UnsignedShort565 << 16 | (uint)TextureFormats.RGBNativeDMP), Formats.RGB565 },
            { ((uint)DataTypes.UnsignedByte44DMP << 16 | (uint)TextureFormats.LuminanceAlphaNativeDMP), Formats.LA44 },
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.LuminanceAlphaNativeDMP), Formats.LA88 },
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.AlphaNativeDMP), Formats.A8 },
            { ((uint)DataTypes.UnsignedByte << 16 | (uint)TextureFormats.LuminanceNativeDMP), Formats.L8 },
            { ((uint)DataTypes.Unsigned4BitsDMP << 16 | (uint)TextureFormats.AlphaNativeDMP), Formats.A4 },
            { ((uint)DataTypes.Unsigned4BitsDMP << 16 | (uint)TextureFormats.LuminanceNativeDMP), Formats.L4 },
        };

        public static Dictionary<Formats, byte> BytesPerPixel = new Dictionary<Formats, byte>()
        {
            { Formats.ETC1, 4 },
            { Formats.ETC1A4, 4 },
            { Formats.RGBA8888, 4 },
            { Formats.RGB888, 3 },
            { Formats.RGBA5551, 2 },
            { Formats.RGB565, 2 },
            { Formats.RGBA4444, 2 },
            { Formats.LA88, 2 },
            { Formats.LA44, 1 },
            { Formats.L8, 1 },
            { Formats.A8, 1 },
        };

        static readonly int[] Convert5To8 =
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39,
            0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B,
            0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD,
            0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        public static void ConvertImage(Formats format, BinaryReader reader, uint numImageBytes, Bitmap image)
        {
            if (format == Formats.ETC1 || format == Formats.ETC1A4)
                DecodeETC1(format, reader, numImageBytes, image);
            else
            {
                BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
                byte[] pixelData = new byte[bmpData.Height * bmpData.Stride];
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                for (int y = 0; y < image.Height; y += 8)
                    for (int x = 0; x < image.Width; x += 8)
                        DecodeTile(8, 8, x, y, ref pixelData, bmpData.Stride, image.Width, reader, format);

                Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                image.UnlockBits(bmpData);
            }
        }

        private static void DecodeETC1(Formats format, BinaryReader reader, uint numImageBytes, Bitmap image)
        {
            try
            {
                /* Get compressed data & handle to it */
                byte[] textureData = reader.ReadBytes((int)numImageBytes);
                ushort[] input = new ushort[textureData.Length / sizeof(ushort)];
                Buffer.BlockCopy(textureData, 0, input, 0, textureData.Length);
                GCHandle pInput = GCHandle.Alloc(input, GCHandleType.Pinned);

                /* Marshal data around, invoke ETC1Lib.dll for conversion, etc */
                UInt32 dataSize = 0;
                UInt16 marshalWidth = (ushort)image.Width, marshalHeight = (ushort)image.Height;
                ConvertETC1(IntPtr.Zero, ref dataSize, IntPtr.Zero, marshalWidth, marshalHeight, (format == Formats.ETC1A4));
                uint[] output = new uint[dataSize];
                GCHandle pOutput = GCHandle.Alloc(output, GCHandleType.Pinned);
                ConvertETC1(pOutput.AddrOfPinnedObject(), ref dataSize, pInput.AddrOfPinnedObject(), marshalWidth, marshalHeight, (format == Formats.ETC1A4));
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
                BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
                byte[] pixelData = new byte[bmpData.Height * bmpData.Stride];
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);

                Buffer.BlockCopy(finalized, 0, pixelData, 0, pixelData.Length);
                for (int i = 0; i < pixelData.Length; i += 4)
                {
                    byte tmp = pixelData[i];
                    pixelData[i] = pixelData[i + 2];
                    pixelData[i + 2] = tmp;
                }

                Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
                image.UnlockBits(bmpData);
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

        private static void DecodeColor(byte[] bytes, Formats format, out int alpha, out int red, out int green, out int blue)
        {
            int val = -1;

            alpha = red = green = blue = 0xFF;

            switch (format)
            {
                case Formats.RGBA8888:
                    val = BitConverter.ToInt32(bytes, 0);
                    red = ((val >> 24) & 0xFF);
                    green = ((val >> 16) & 0xFF);
                    blue = ((val >> 8) & 0xFF);
                    alpha = (val & 0xFF);
                    break;

                case Formats.RGB888:
                    red = bytes[2];
                    green = bytes[1];
                    blue = bytes[0];
                    break;

                case Formats.RGBA5551:
                    val = BitConverter.ToInt16(bytes, 0);
                    red = Convert5To8[(val >> 11) & 0x1F];
                    green = Convert5To8[(val >> 6) & 0x1F];
                    blue = Convert5To8[(val >> 1) & 0x1F];
                    alpha = (val & 0x0001) == 1 ? 0xFF : 0x00;
                    break;

                case Formats.RGB565:
                    val = BitConverter.ToInt16(bytes, 0);
                    red = Convert5To8[(val >> 11) & 0x1F];
                    green = ((val >> 5) & 0x3F) * 4;
                    blue = Convert5To8[val & 0x1F];
                    break;

                case Formats.RGBA4444:
                    val = BitConverter.ToInt16(bytes, 0);
                    red = (((val >> 12) << 4) & 0xFF);
                    green = (((val >> 8) << 4) & 0xFF);
                    blue = (((val >> 4) << 4) & 0xFF);
                    alpha = ((val << 4) & 0xFF);
                    break;

                case Formats.LA88:
                    val = BitConverter.ToInt16(bytes, 0);
                    red = green = blue = ((val >> 8) & 0xFF);
                    alpha = (val & 0xFF);
                    break;

                case Formats.LA44:
                    val = bytes[0];
                    red = green = blue = (((val >> 4) << 4) & 0xFF);
                    alpha = (((val & 0xF) << 4) & 0xFF);
                    break;

                case Formats.L8:
                    alpha = 0xFF;
                    red = green = blue = bytes[0];
                    break;

                case Formats.A8:
                    alpha = bytes[0];
                    red = green = blue = 0xFF;
                    break;
            }
        }

        private static void DecodeTile(int iconSize, int tileSize, int ax, int ay, ref byte[] pixelData, int stride, int width, BinaryReader reader, Formats format)
        {
            if (tileSize == 0)
            {
                byte[] bytes = new byte[BytesPerPixel[format]];
                Buffer.BlockCopy(reader.ReadBytes(bytes.Length), 0, bytes, 0, bytes.Length);

                int alpha, red, green, blue;
                DecodeColor(bytes, format, out alpha, out red, out green, out blue);

                pixelData[(ay * stride) + (ax * (stride / width)) + 2] = (byte)red;
                pixelData[(ay * stride) + (ax * (stride / width)) + 1] = (byte)green;
                pixelData[(ay * stride) + (ax * (stride / width))] = (byte)blue;
                pixelData[(ay * stride) + (ax * (stride / width)) + 3] = (byte)alpha;
            }
            else
                for (var y = 0; y < iconSize; y += tileSize)
                    for (var x = 0; x < iconSize; x += tileSize)
                        DecodeTile(tileSize, tileSize / 2, x + ax, y + ay, ref pixelData, stride, width, reader, format);
        }
    }
}
