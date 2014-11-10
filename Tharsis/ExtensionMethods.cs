using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Tharsis
{
    [Flags]
    enum PathType
    {
        Undetermined = 0,
        FileFound = 1 << 0,
        DirectoryFound = 1 << 1,
        FileInvalid = 1 << 2,
        DirectoryInvalid = 1 << 3,
        ContainsFilter = 1 << 4
    };

    [Flags]
    enum LineType
    {
        Overline = 1 << 0,
        Underline = 1 << 1
    };

    static class ExtensionMethods
    {
        /* https://stackoverflow.com/a/1387355 */
        public static string[] ParseArguments(this string[] args)
        {
            string exp = @"^(?:""([^""]*)""\s*|([^""\s]+)\s*)+";
            Match m = Regex.Match(Environment.CommandLine, exp);
            if (m.Groups.Count < 2) Program.PrintUsageExit();

            var captures = m.Groups[1].Captures.Cast<Capture>().Concat(m.Groups[2].Captures.Cast<Capture>()).OrderBy(x => x.Index).ToArray();
            if (captures.Length < 2) Program.PrintUsageExit();

            List<string> parsed = new List<string>();
            foreach (Capture capture in captures) parsed.Add(capture.Value);

            return parsed.ToArray();
        }

        public static string Center(this string str, int padding = 0)
        {
            string[] split = str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            int maxLen = (split.Max(x => x.Length)) + padding;
            for (int i = 0; i < split.Length; i++)
            {
                int width = ((maxLen / 2) + ((split[i].Length + padding) - 1) / 2) + 1;
                split[i] = split[i].PadLeft(width).PadRight(maxLen + padding);
            }
            return string.Join("\r\n", split);
        }

        public static string StyleLine(this string str, LineType linetype)
        {
            string[] split = str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            int maxLen = split.Max(x => x.Length);
            StringBuilder builder = new StringBuilder();

            if ((linetype & LineType.Overline) != 0) builder.Append("".PadRight(maxLen, '-') + Environment.NewLine);
            builder.AppendLine(str);
            if ((linetype & LineType.Underline) != 0) builder.Append("".PadRight(maxLen, '-'));

            return builder.ToString();
        }

        public static PathType ExaminePath(this string path)
        {
            PathType result = PathType.Undetermined;

            try
            {
                if (Path.GetFileName(path).Contains('*'))
                {
                    result = PathType.ContainsFilter;
                    path = Path.GetDirectoryName(path);
                }

                if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
                    result |= PathType.DirectoryFound;
                else
                    result |= PathType.FileFound;
            }
            catch (DirectoryNotFoundException)
            {
                return PathType.DirectoryInvalid;
            }
            catch (FileNotFoundException)
            {
                return PathType.FileInvalid;
            }

            return result;
        }
    }
}
