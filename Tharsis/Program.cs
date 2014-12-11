using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Tharsis
{
    class Program
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        public static List<Switch> Switches { get; private set; }

        public static string ApplicationPath { get; private set; }
        public static Version ApplicationVersion { get; private set; }

        public static string InputPath { get; private set; }

        static PathType inputType;
        static string inputFilter;
        static string outputPath;
        static int successCounter, failureCounter;

        public static bool NoDeepScan = true;
        public static bool KeepExistingFiles = false;
        public static bool ConvertTMXIndexed = false;

        static void Main(string[] args)
        {
            ApplicationPath = Assembly.GetExecutingAssembly().Location;
            ApplicationVersion = Version.Parse((Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)[0] as AssemblyFileVersionAttribute).Version);
            Console.Title = Path.GetFileName(ApplicationPath);

            StringBuilder headerString = new StringBuilder();
            headerString.AppendFormat("Tharsis {0}.{1} - Etrian Odyssey IV File Converter\n", ApplicationVersion.Major, ApplicationVersion.Minor);
            headerString.Append("Written 2014 by xdaniel - http://magicstone.de/dzd/\n");
            headerString.Append("ETC1 support based on rg_etc1 by Rich Geldreich");

            Console.WriteLine(headerString.ToString().Center(2).StyleLine(LineType.Overline | LineType.Underline));
            Console.WriteLine();

            VerifyETC1Library();

            InitializeSwitches();
            VerifyArguments(args.ParseArguments());

            List<Type> knownFileTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.BaseType == typeof(BaseFile) && x.GetCustomAttributes(typeof(FileExtensionsAttribute), false).Length != 0)
                .OrderBy(x => x.Name)
                .ToList();

            successCounter = failureCounter = 0;

            Stopwatch timeTaken = new Stopwatch();
            timeTaken.Start();

            if ((inputType & PathType.FileFound) != 0)
            {
                Type fileType = null;
                string extension = Path.GetExtension(InputPath);

                foreach (Type type in knownFileTypes)
                {
                    FileExtensionsAttribute fileExtAttrib = (type.GetCustomAttributes(typeof(FileExtensionsAttribute), false)[0] as FileExtensionsAttribute);
                    if (fileExtAttrib.SourceExtension.ToLowerInvariant() == extension.ToLowerInvariant())
                    {
                        fileType = type;
                        if (outputPath == null)
                            outputPath = Path.Combine(Path.GetDirectoryName(InputPath), Path.GetFileName(InputPath) + fileExtAttrib.TargetExtension);
                        else if (Path.GetFileName(outputPath) == string.Empty)
                            outputPath = Path.Combine(outputPath, Path.GetFileName(InputPath) + fileExtAttrib.TargetExtension);
                        break;
                    }
                }

                string displayInput = InputPath.Replace(Path.GetDirectoryName(InputPath), "").TrimStart(Path.DirectorySeparatorChar);
                if (!Directory.Exists(Path.GetDirectoryName(outputPath))) Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                if ((File.Exists(outputPath) && KeepExistingFiles) || fileType == null)
                    Console.WriteLine("Skipping {0}...", displayInput);
                else
                {
                    Console.Write("Converting {0}...", displayInput);

                    BaseFile instance = (Activator.CreateInstance(fileType, new object[] { InputPath }) as BaseFile);
                    if (instance.Save(outputPath))
                    {
                        Console.WriteLine("done.");
                        successCounter++;
                    }
                    else
                    {
                        Console.WriteLine("failed!");
                        failureCounter++;
                    }
                }
                Console.WriteLine();
            }
            else if ((inputType & PathType.DirectoryFound) != 0)
            {
                foreach (Type type in knownFileTypes)
                {
                    FileExtensionsAttribute fileExtAttrib = (type.GetCustomAttributes(typeof(FileExtensionsAttribute), false)[0] as FileExtensionsAttribute);
                    string fileFilter = "*" + fileExtAttrib.SourceExtension;

                    if ((inputType & PathType.ContainsFilter) != 0 && inputFilter != null)
                    {
                        if (inputFilter != "*.*" && inputFilter != fileFilter) continue;
                    }

                    Console.WriteLine(string.Format("Processing {0}s...", type.Name).StyleLine(LineType.Overline | LineType.Underline));
                    List<string> files = Directory.EnumerateFiles(InputPath, fileFilter, (NoDeepScan ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)).ToList();
                    if (files.Count > 0)
                    {
                        foreach (string file in files)
                        {
                            string outputFile = (outputPath == null ?
                                Path.Combine(Path.GetDirectoryName(file), "Converted", Path.GetFileName(file) + fileExtAttrib.TargetExtension) :
                                Path.Combine(Path.GetDirectoryName(file.Replace(InputPath, outputPath + Path.DirectorySeparatorChar)), Path.GetFileName(file) + fileExtAttrib.TargetExtension));

                            string displayInput = file.Replace(InputPath, "").TrimStart(Path.DirectorySeparatorChar);
                            if (!Directory.Exists(Path.GetDirectoryName(outputFile))) Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                            if (File.Exists(outputFile) && KeepExistingFiles)
                                Console.WriteLine("Skipping {0}...", displayInput);
                            else
                            {
                                Console.Write("Converting {0}...", displayInput);

                                BaseFile instance = (Activator.CreateInstance(type, new object[] { file }) as BaseFile);
                                if (instance.Save(outputFile))
                                {
                                    Console.WriteLine("done.");
                                    successCounter++;
                                }
                                else
                                {
                                    Console.WriteLine("failed!");
                                    failureCounter++;
                                }
                            }
                        }
                    }
                    else
                        Console.WriteLine("No files found!");

                    Console.WriteLine();
                }
            }

            timeTaken.Stop();

            Console.WriteLine("Converted {0} file{1}, conversion failed on {2} file{3}.", successCounter, (successCounter == 1 ? string.Empty : "s"), failureCounter, (failureCounter == 1 ? string.Empty : "s"));
            Console.WriteLine("Processing finished after {0}.", timeTaken.Elapsed.ToReadableString());
            Console.WriteLine();

            Console.WriteLine("Done, press any key to exit!");
            Console.ReadKey();
        }

        public static void PrintUsageExit(int exitCode = 0)
        {
            string appFileName = Path.GetFileNameWithoutExtension(ApplicationPath);
            Console.WriteLine("Usage: {0} <input> [options]", appFileName);
            Console.WriteLine();
            ArgsHandling.PrintOptionHelp(Switches);
            WaitForExit(exitCode);
        }

        public static void WaitForExit(int exitCode = 0)
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(exitCode);
        }

        private static void VerifyETC1Library()
        {
            string etc1LibraryPath = Path.Combine(Path.GetDirectoryName(ApplicationPath), "ETC1Lib.dll");
            if (!File.Exists(etc1LibraryPath))
            {
                Console.WriteLine("Error: 'ETC1Lib.dll' not found!");
                Console.WriteLine();
                WaitForExit(-2);
            }
            else if (GetProcAddress(LoadLibrary(etc1LibraryPath), "ConvertETC1") == UIntPtr.Zero)
            {
                Console.WriteLine("Error: Invalid 'ETC1Lib.dll' detected!");
                Console.WriteLine();
                WaitForExit(-2);
            }
        }

        private static void InitializeSwitches()
        {
            Switches = new List<Switch>();
            Switches.Add(new Switch("output", "Specify output directory or filename for converted file(s)", new Func<string[], int, int>((arguments, index) =>
            {
                if (index + 1 < arguments.Length) outputPath = arguments[index + 1];
                return (index + 1);
            }), shortOption: "o", argsHelp: "<path>"));

            Switches.Add(new Switch("nosubdir", "Do not search all sub-directories inside input directory", new Func<string[], int, int>((arguments, index) =>
            {
                NoDeepScan = false;
                return index;
            }), shortOption: "n"));

            Switches.Add(new Switch("keep", "Do not overwrite converted files that already exist", new Func<string[], int, int>((arguments, index) =>
            {
                KeepExistingFiles = true;
                return index;
            }), shortOption: "k"));

            Switches.Add(new Switch("ascii", "Transform any Shift-JIS roman characters in message files to ASCII characters", new Func<string[], int, int>((arguments, index) =>
            {
                EO4String.TransformToAscii = true;
                return index;
            }), shortOption: "a"));

            Switches.Add(new Switch("colorindex", "Convert TMX files to indexed color images, preserving the palette and color order", new Func<string[], int, int>((arguments, index) =>
            {
                ConvertTMXIndexed = true;
                return index;
            }), shortOption: "c"));

            Switches.Add(new Switch("help", "Show this help message", new Func<string[], int, int>((arguments, index) =>
            {
                PrintUsageExit();
                return index;
            }), shortOption: "h"));
        }

        private static void VerifyArguments(string[] arguments)
        {
            if (arguments.Length < 2) PrintUsageExit();

            InputPath = arguments[1];
            inputType = InputPath.ExaminePath();

            if ((inputType & PathType.DirectoryInvalid) != 0)
            {
                Console.WriteLine("Error: Directory '{0}' does not exist!", InputPath);
                Console.WriteLine();
                PrintUsageExit(-1);
            }
            else if ((inputType & PathType.FileInvalid) != 0)
            {
                Console.WriteLine("Error: File '{0}' does not exist!", InputPath);
                Console.WriteLine();
                PrintUsageExit(-1);
            }

            if ((inputType & PathType.ContainsFilter) != 0)
            {
                inputFilter = Path.GetFileName(InputPath);
                InputPath = Path.GetDirectoryName(InputPath);
            }

            if (arguments.Length > 2)
                ArgsHandling.CheckOptions(Switches, arguments, 2);
        }
    }
}
