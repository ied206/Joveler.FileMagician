using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Joveler.FileMagician.Samples
{
    #region (CommandLineParser) ParamOptions
    public abstract class ParamOptions
    {
        [Option('m', "magic-file", Required = false, Default = null, HelpText = "Magic number file.")]
        public string? MagicFile { get; set; }
        [Option('v', "version", Required = false, Default = false, HelpText = "Display version information.")]
        public bool Version { get; set; }
    }

    [Verb("detect", HelpText = "Determine file type.")]
    public class FileDetectOptions : ParamOptions
    {
        [Option("extension", Required = false, Default = false, HelpText = "Output extension.")]
        public bool OutputExtension { get; set; }
        [Option("mime-type", Required = false, Default = false, HelpText = "Output MIME type.")]
        public bool OutputMimeType { get; set; }
        [Option("mime-encoding", Required = false, Default = false, HelpText = "Output MIME encoding.")]
        public bool OutputMimeEncoding { get; set; }
        [Value(0, HelpText = "FILEs to insepct.")]
        public IEnumerable<string> TargetFiles { get; set; } = Array.Empty<string>();
    }

    [Verb("compile", HelpText = "Compile file specified by -m.")]
    public class MagicCompileOptions : ParamOptions
    {
    }
    #endregion

    #region MagicEntry
    public class MagicEntry
    {
        public string Target { get; private set; }
        public string DisplayName { get; private set; }
        public string Output { get; set; }

        public MagicEntry(string target, string displayName)
        {
            Target = target;
            DisplayName = displayName;
            Output = string.Empty;
        }
    }
    #endregion

    #region Program
    public class Program
    {
        public static string BaseDir { get; set; } = string.Empty;
        public static string MagicFileMgc { get; set; } = string.Empty;
        public static string MagicFileSrc { get; set; } = string.Empty;
        private static ParserResult<object>? _parserResult = null;

        #region PrintErrorAndExit
        internal static void PrintErrorAndExit(IEnumerable<Error> errs)
        {
            foreach (Error err in errs)
            {
                switch (err)
                {
                    case HelpRequestedError:
                    case HelpVerbRequestedError:
                    case VersionRequestedError:
                        break;
                    default:
                        Console.WriteLine(err.ToString());
                        break;
                }
            }
            Environment.Exit(1);
        }
        #endregion

        #region BuildHelpMessage
        public static string BuildHelpMessage(string? appendMessage = null)
        {
            HelpText helpText = HelpText.AutoBuild(_parserResult, h =>
            {
                h.AddNewLineBetweenHelpSections = true;
                if (appendMessage != null)
                    h.AddPreOptionsText(appendMessage);
                return h;
            });
            return helpText.ToString();
        }
        #endregion

        #region Init and Cleanup
        public static void NativeGlobalInit()
        {
            BaseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
            MagicFileMgc = Path.Combine(BaseDir, "magic.mgc");
            MagicFileSrc = Path.Combine(BaseDir, "magic.src");

            string libDir = Path.Combine(BaseDir, "runtimes");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libDir = Path.Combine(libDir, "win-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libDir = Path.Combine(libDir, "linux-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libDir = Path.Combine(libDir, "osx-");

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    libDir += "x86";
                    break;
                case Architecture.X64:
                    libDir += "x64";
                    break;
                case Architecture.Arm:
                    libDir += "arm";
                    break;
                case Architecture.Arm64:
                    libDir += "arm64";
                    break;
            }
            libDir = Path.Combine(libDir, "native");

            string? libName = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libName = "libmagic-1.dll";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libName = "libmagic.so";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libName = "libmagic.dylib";
            else
                throw new PlatformNotSupportedException($"Unable to find native library.");

            string libPath = Path.Combine(libDir, libName);

            if (File.Exists(libPath))
                Magic.GlobalInit(libPath);
            else if (File.Exists(libName))
                Magic.GlobalInit(libName); // Try the same directory with executable
            else
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");
        }

        public static void NativeGlobalCleanup()
        {
            Magic.GlobalCleanup();
        }
        #endregion

        public static int Main(string[] args)
        {
            NativeGlobalInit();

            ParamOptions? opts = null;
            Parser argParser = new Parser(conf =>
            {
                conf.HelpWriter = Console.Out;
                conf.CaseInsensitiveEnumValues = true;
                conf.CaseSensitive = false;
            });

            _parserResult = argParser.ParseArguments<FileDetectOptions, MagicCompileOptions>(args);
            _parserResult.WithParsed<FileDetectOptions>(x => opts = x)
                .WithParsed<MagicCompileOptions>(x => opts = x)
                .WithNotParsed(errs => PrintErrorAndExit(errs));

            if (opts == null)
                throw new InvalidOperationException("Argument parsing failed.");

            switch (opts)
            {
                case FileDetectOptions detectOpts:
                    CheckFile(detectOpts);
                    break;
                case MagicCompileOptions compileOpts:
                    CompileFile(compileOpts);
                    break;
            }

            NativeGlobalCleanup();
            return 0;
        }

        #region Check File
        public static void CheckFile(FileDetectOptions opts)
        {
            // Process magicFile
            string magicFile = opts.MagicFile ?? MagicFileMgc;

            // Process magicFlags
            MagicFlags magicFlags = MagicFlags.None;
            if (opts.OutputExtension)
                magicFlags |= MagicFlags.Extension;
            else if (opts.OutputMimeType)
                magicFlags |= MagicFlags.MimeType;
            else if (opts.OutputMimeEncoding)
                magicFlags |= MagicFlags.MimeEncoding;

            // Process target files
            List<MagicEntry> targetFiles = new List<MagicEntry>();
            string[] rawTargetFiles = opts.TargetFiles.ToArray();
            if (rawTargetFiles.Length == 0)
            {
                Console.WriteLine($"No target file or directory");
                Environment.Exit(1);
            }
            
            char[] wildcardAnyOf = new char[] { '*', '?' };
            foreach (string rawTargetFile in rawTargetFiles)
            {
                if (rawTargetFile.IndexOfAny(wildcardAnyOf) != -1)
                { // wildcard
                    string fullPath = Path.GetFullPath(rawTargetFile);
                    string? dirPath = Path.GetDirectoryName(fullPath);
                    if (dirPath == null)
                        continue;
                    string fileName = Path.GetFileName(fullPath);

                    try
                    {
                        IEnumerable<string> fsEntries = Directory.EnumerateFileSystemEntries(dirPath, fileName, SearchOption.TopDirectoryOnly);
                        targetFiles.AddRange(fsEntries.Select(x => new MagicEntry(x, x[(dirPath.Length + 1)..])));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Cannot glob [{rawTargetFile}]: {e.Message}");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    string fullPath = Path.GetFullPath(rawTargetFile);
                    targetFiles.Add(new MagicEntry(fullPath, rawTargetFile));
                }
            }

            if (targetFiles.Count == 0)
            {
                Console.WriteLine($"No target file or directory");
                Environment.Exit(1);
            }

            int maxPathSize = Math.Min(targetFiles.Max(x => x.DisplayName.Length), 60);
            List<MagicEntry> results = new List<MagicEntry>(targetFiles.Count);
            using (Magic magic = Magic.Open(magicFile, magicFlags))
            {
                foreach (MagicEntry entry in targetFiles)
                {
                    string output;
                    try
                    {
                        if (Directory.Exists(entry.Target))
                            output = "directory";
                        else if (File.Exists(entry.Target))
                            output = magic.CheckFile(entry.Target);
                        else
                            output = "No such file or directory";
                    }
                    catch (Exception e)
                    {
                        output = $"Cannot open [{entry.DisplayName}]: {e.Message}";
                    }
                    entry.Output = output;
                }
            }

            // Print output
            foreach (MagicEntry entry in targetFiles)
            {
                ConsoleColor cc = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{entry.DisplayName.PadRight(maxPathSize)}:");
                    Console.ForegroundColor = cc;
                    Console.WriteLine($" {entry.Output}");
                }
                finally
                {
                    Console.ForegroundColor = cc;
                }
            }
        }
        #endregion

        #region Compile File
        public static void CompileFile(MagicCompileOptions opts)
        {
            // Process magicFile
            if (opts.MagicFile == null)
            {
                Console.WriteLine($"No magic database source specified.");
                Environment.Exit(1);
            }

            using (Magic magic = Magic.Open(MagicFlags.None))
            {
                magic.Compile(opts.MagicFile);
            }
            Console.WriteLine($"Successfully compiled magic database source.");
        }
        #endregion
    }
    #endregion
}
