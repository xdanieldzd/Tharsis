using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tharsis
{
    public class Switch
    {
        public string Option { get; private set; }
        public string ShortOption { get; private set; }
        public string ArgsHelp { get; private set; }
        public string HelpText { get; private set; }
        public Func<string[], int, int> Function { get; private set; }

        public Switch(string option, string helpText, Func<string[], int, int> function, string shortOption = "", string argsHelp = "")
        {
            Option = option;
            ShortOption = shortOption;
            ArgsHelp = argsHelp;
            HelpText = helpText;
            Function = function;
        }
    }

    public static class ArgsHandling
    {
        public static void CheckOptions(List<Switch> switches, string[] arguments, int startIndex)
        {
            string[] prefixes = new string[] { "-", "--", "/" };
            for (int i = startIndex; i < arguments.Length; i++)
            {
                foreach (string prefix in prefixes.OrderByDescending(x => x.Length))
                {
                    if (arguments[i].StartsWith(prefix))
                    {
                        string check = arguments[i].Substring(prefix.Length).ToLowerInvariant();
                        foreach (Switch optSwitch in switches)
                        {
                            if (check == optSwitch.Option.ToLowerInvariant() || check == optSwitch.ShortOption.ToLowerInvariant())
                            {
                                i = optSwitch.Function(arguments, i);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void PrintOptionHelp(List<Switch> switches)
        {
            int maxSwitchLen = (switches.Select(x => (x.Option.Length + 2) + (x.ShortOption != string.Empty ? x.ShortOption.Length + 4 : 0) + x.ArgsHelp.Length).Max());

            Console.WriteLine("Options:");
            foreach (Switch optSwitch in switches)
            {
                string shortOpt = (optSwitch.ShortOption != string.Empty ? string.Format("-{0}, ", optSwitch.ShortOption) : "");
                string startString = string.Format("  {0}--{1} {2}", shortOpt, optSwitch.Option, optSwitch.ArgsHelp);
                startString += new string(' ', maxSwitchLen - (optSwitch.Option.Length + shortOpt.Length + optSwitch.ArgsHelp.Length));

                List<string> helpStrings = Wrap(optSwitch.HelpText, Console.BufferWidth - startString.Length - 3);

                Console.Write(startString);
                foreach (string help in helpStrings)
                {
                    Console.WriteLine(help);
                    if (help != helpStrings.Last()) Console.Write(new string(' ', startString.Length));
                }
            }
            Console.WriteLine();
        }

        /* http://bryan.reynoldslive.com/post/Wrapping-string-data.aspx */
        private static List<String> Wrap(string text, int maxLength)
        {
            if (text.Length == 0) return new List<string>();

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var currentWord in words)
            {
                if ((currentLine.Length > maxLength) || ((currentLine.Length + currentWord.Length) > maxLength))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                if (currentLine.Length > 0) currentLine += " " + currentWord;
                else currentLine += currentWord;
            }

            if (currentLine.Length > 0) lines.Add(currentLine);

            return lines;
        }
    }
}
