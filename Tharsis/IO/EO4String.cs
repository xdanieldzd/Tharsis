using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.IO
{
    class EO4String : Encoding
    {
        static EO4String instance;
        public static EO4String GetInstance()
        {
            if (instance == null) instance = new EO4String();
            return instance;
        }

        static Encoding sjis = GetEncoding(932);

        public static bool TransformToAscii { get; set; }
        static Dictionary<char, char> asciiTranslator = new Dictionary<char, char>()
        {
            { '　', ' ' }, { '，', ',' }, { '．', '.' }, { '：', ':' }, { '；', ';' }, { '？', '?' }, { '！', '!' }, { '－', '-' }, { '／', '/' }, { '～', '~' }, { '’', '\'' }, { '”', '\"' }, { '（', '(' }, { '）', ')' }, 
            { '０', '0' }, { '１', '1' }, { '２', '2' }, { '３', '3' }, { '４', '4' }, { '５', '5' }, { '６', '6' }, { '７', '7' }, { '８', '8' }, { '９', '9' },
            { 'Ａ', 'A' }, { 'Ｂ', 'B' }, { 'Ｃ', 'C' }, { 'Ｄ', 'D' }, { 'Ｅ', 'E' }, { 'Ｆ', 'F' }, { 'Ｇ', 'G' }, { 'Ｈ', 'H' }, { 'Ｉ', 'I' }, { 'Ｊ', 'J' }, { 'Ｋ', 'K' }, { 'Ｌ', 'L' }, { 'Ｍ', 'M' }, { 'Ｎ', 'N' }, { 'Ｏ', 'O' },
            { 'Ｐ', 'P' }, { 'Ｑ', 'Q' }, { 'Ｒ', 'R' }, { 'Ｓ', 'S' }, { 'Ｔ', 'T' }, { 'Ｕ', 'U' }, { 'Ｖ', 'V' }, { 'Ｗ', 'W' }, { 'Ｘ', 'X' }, { 'Ｙ', 'Y' }, { 'Ｚ', 'Z' },
            { 'ａ', 'a' }, { 'ｂ', 'b' }, { 'ｃ', 'c' }, { 'ｄ', 'd' }, { 'ｅ', 'e' }, { 'ｆ', 'f' }, { 'ｇ', 'g' }, { 'ｈ', 'h' }, { 'ｉ', 'i' }, { 'ｊ', 'j' }, { 'ｋ', 'k' }, { 'ｌ', 'l' }, { 'ｍ', 'm' }, { 'ｎ', 'n' }, { 'ｏ', 'o' }, 
            { 'ｐ', 'p' }, { 'ｑ', 'q' }, { 'ｒ', 'r' }, { 'ｓ', 's' }, { 'ｔ', 't' }, { 'ｕ', 'u' }, { 'ｖ', 'v' }, { 'ｗ', 'w' }, { 'ｘ', 'x' }, { 'ｙ', 'y' }, { 'ｚ', 'z' }, 
        };

        static EO4String()
        {
            TransformToAscii = false;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return sjis.GetMaxCharCount(byteCount);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return sjis.GetMaxByteCount(charCount);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return sjis.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return sjis.GetCharCount(bytes, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return sjis.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return sjis.GetByteCount(chars, index, count);
        }

        public override string GetString(byte[] bytes)
        {
            return this.GetString(bytes, 0, bytes.Length);
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = index; i < index + count; i += 2)
            {
                if ((bytes[i] & 0xF0) == 0xF0 || bytes[i] == 0x80)
                {
                    /* Control code */
                    var arg = 0;
                    switch (bytes[i + 1])
                    {
                        case 0x01:
                            if (bytes[i + 3] == 0x02)
                            {
                                /* Page break */
                                builder.AppendLine();
                                i += 2;
                            }
                            else
                                /* Line break */
                                builder.AppendLine();
                            break;

                        case 0x04:
                            arg = (ushort)((bytes[i + 3] & 0xF) << 8 | bytes[i + 2]);
                            builder.AppendFormat("[Color:0x{0:X4}]", arg);
                            i += 2;
                            break;

                        case 0x11:
                            arg = (ushort)((bytes[i + 3] & 0xF) << 8 | bytes[i + 2]);
                            builder.AppendFormat("[Var?:0x{0:X4}]", arg);
                            i += 2;
                            break;

                        case 0x40: builder.Append("[Guild Name]"); break;

                        case 0x41:
                            arg = (ushort)((bytes[i + 3] & 0xF) << 8 | bytes[i + 2]);
                            builder.AppendFormat("[Item:0x{0:X4}]", arg);
                            i += 2;
                            break;

                        case 0x42:
                            arg = (ushort)((bytes[i + 3] & 0xF) << 8 | bytes[i + 2]);
                            builder.AppendFormat("[Enemy:0x{0:X4}]", arg);
                            i += 2;
                            break;

                        case 0x43:
                            arg = (ushort)((bytes[i + 3] & 0xF) << 8 | bytes[i + 2]);
                            builder.AppendFormat("[Character?:0x{0:X4}]", arg);
                            i += 2;
                            break;

                        case 0x44: builder.Append("[Skyship Name]"); break;

                        case 0xFF: builder.Append("[End]"); break;

                        default: builder.AppendFormat("[{0:X2}{1:X2}]", bytes[i], bytes[i + 1]); break;
                    }
                }
                else
                {
                    char[] str = sjis.GetChars(bytes, i, 2);
                    if (TransformToAscii && asciiTranslator.ContainsKey(str[0])) builder.Append(asciiTranslator[str[0]]);
                    else builder.Append(str);
                }
            }

            return builder.ToString();
        }
    }
}
