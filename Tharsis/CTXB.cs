﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace Tharsis
{
    [FileExtensions(".ctxb", ".png")]
    public class CTXB : BaseFile
    {
        public CTXB(string path) : base(path) { }

        /* ctxb */
        public string FileTag { get; private set; }
        public uint FileSize { get; private set; }
        public uint NumberOfChunks { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint TexChunkOffset { get; private set; }
        public uint TextureDataOffset { get; private set; }

        /* tex */
        public string TexChunkTag { get; private set; }
        public uint TexChunkSize { get; private set; }
        public uint TextureCount { get; private set; }
        public Texture[] Textures { get; private set; }

        protected override void Parse(BinaryReader reader)
        {
            FileTag = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            FileSize = reader.ReadUInt32();
            NumberOfChunks = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            TexChunkOffset = reader.ReadUInt32();
            TextureDataOffset = reader.ReadUInt32();

            if (FileTag != "ctxb") return;

            reader.BaseStream.Seek(TexChunkOffset, SeekOrigin.Begin);

            TexChunkTag = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            TexChunkSize = reader.ReadUInt32();
            TextureCount = reader.ReadUInt32();

            if (TexChunkTag != "tex " || TextureCount != 1) return;

            Textures = new Texture[TextureCount];
            for (int i = 0; i < Textures.Length; i++)
            {
                reader.BaseStream.Seek(TexChunkOffset + 0xC + (i * 0x24), SeekOrigin.Begin);
                Textures[i] = new Texture(this, reader);
            }
        }

        public override bool Save(string path)
        {
            if (Textures != null && Textures.FirstOrDefault().TexImage != null)
            {
                Textures.FirstOrDefault().TexImage.Save(path);
                return true;
            }
            else
                return false;
        }

        public class Texture
        {
            public uint DataLength { get; private set; }
            public ushort Unknown04 { get; private set; }
            public ushort Unknown06 { get; private set; }
            public ushort Width { get; private set; }
            public ushort Height { get; private set; }
            public uint RawFormat { get; private set; }
            public uint DataOffset { get; private set; }
            public string Name { get; private set; }

            public Bitmap TexImage { get; private set; }

            public Texture(CTXB parent, BinaryReader reader)
            {
                DataLength = reader.ReadUInt32();
                Unknown04 = reader.ReadUInt16();
                Unknown06 = reader.ReadUInt16();
                Width = reader.ReadUInt16();
                Height = reader.ReadUInt16();
                RawFormat = reader.ReadUInt32();
                DataOffset = reader.ReadUInt32();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(16), 0, 16).TrimEnd('\0');

                TextureCommons.Formats format = TextureCommons.FormatMap[RawFormat];
                if (!TextureCommons.BytesPerPixel.ContainsKey(format)) return;

                TexImage = new Bitmap(Width, Height);

                reader.BaseStream.Seek(parent.TextureDataOffset + DataOffset, SeekOrigin.Begin);
                TextureCommons.ConvertImage(format, reader, DataLength, TexImage);
            }
        }
    }
}
