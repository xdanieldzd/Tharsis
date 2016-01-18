using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Tharsis.Images
{
    public static class Texture
    {
        public static Bitmap ToBitmap(Pica.DataTypes dataType, Pica.PixelFormats pixelFormat, int width, int height, Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream);

            return ToBitmap(dataType, pixelFormat, width, height, reader);
        }

        public static Bitmap ToBitmap(Pica.DataTypes dataType, Pica.PixelFormats pixelFormat, int width, int height, byte[] data)
        {
            using (MemoryStream inputStream = new MemoryStream(data))
            {
                return ToBitmap(dataType, pixelFormat, width, height, new BinaryReader(inputStream));
            }
        }

        public static Bitmap ToBitmap(Pica.DataTypes dataType, Pica.PixelFormats pixelFormat, int width, int height, BinaryReader reader)
        {
            TileDecoderDelegate decoder = TileCodecs.GetDecoder(dataType, pixelFormat);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte[] targetData = new byte[bmpData.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, targetData, 0, targetData.Length);

            for (int y = 0; y < height; y += 8)
                for (int x = 0; x < width; x += 8)
                    decoder(reader, targetData, x, y, (int)width, (int)height);

            Marshal.Copy(targetData, 0, bmpData.Scan0, targetData.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }

        public static byte[] ToBytes(Bitmap bitmap, Pica.DataTypes dataType, Pica.PixelFormats pixelFormat)
        {
            MemoryStream output = new MemoryStream(32);
            BinaryWriter writer = new BinaryWriter(output);

            TileEncoderDelegate encoder = TileCodecs.GetEncoder(dataType, pixelFormat);

            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte[] sourceData = new byte[bmpData.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, sourceData, 0, sourceData.Length);

            for (int y = 0; y < bitmap.Height; y += 8)
                for (int x = 0; x < bitmap.Width; x += 8)
                    encoder(writer, sourceData, x, y, bitmap.Width, bitmap.Height);

            Marshal.Copy(sourceData, 0, bmpData.Scan0, sourceData.Length);
            bitmap.UnlockBits(bmpData);

            return output.ToArray();
        }
    }
}
