using ByteSizeLib;
using CrashReporterDotNET;
using ExceptionReporting;
using ExceptionReporting.Network.Events;
using LeagueFPSBoost.Configuration;
using LeagueFPSBoost.Cryptography;
using LeagueFPSBoost.Diagnostics.Debugger;
using LeagueFPSBoost.Extensions;
using LeagueFPSBoost.GUI;
using LeagueFPSBoost.Logging;
using LeagueFPSBoost.Native;
using LeagueFPSBoost.Native.Unmanaged;
using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Properties;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater;
using LeagueFPSBoost.Updater.MessageBoxCollection;
using LeagueFPSBoost.Updater.PostUpdateAction;
using Microsoft.Win32;
using NAudio.Wave;
using NDesk.Options;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using PowerManagerAPI;
using StringCipherLib;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeagueFPSBoost
{
    static class Program
    {
#if DEBUG
        public static readonly bool DebugBuild = true;
#else
        public static readonly bool DebugBuild = false;
#endif

        public static readonly DebuggerWatcher DebuggerWatcher = new DebuggerWatcher();

        public static DateTime StartTime { get; private set; } = DateTime.Now;
        public static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public static Mutex Mutex { get; private set; }

        public static int CodeStep { get; private set; }

        public static event EventHandler<CrashUploadReportEventArgs> OnCrashUploadReport = delegate { };



        public static string LeaguePath { get; private set; }
        public static string LeagueGamePath { get; private set; }
        public static string LeagueLogFileDirPath { get; private set; }
        public static string LeagueConfigDirPath { get; private set; }
        public static string AppConfigDir { get; set; }

        public static ManagementEventWatcher StartWatch { get; private set; }
        public static ManagementEventWatcher StopWatch { get; private set; }
        
        public static List<string> PreLogWarnings = new List<string>();
        public static List<string> PreNLogMessages = new List<string>();
        public static StringBuilder CrashSb = new StringBuilder();

        public static StringBuilder PiSB = new StringBuilder();
        public static StringBuilder OsSB = new StringBuilder();
        public static StringBuilder CpuSB = new StringBuilder();
        public static StringBuilder GpuSB = new StringBuilder();

        public static bool PlayNotiAllow;

        public static bool PathFound { get; private set; }

        public static string LeagueClientInfo = "";

        public static Logger Logger { get; private set; } = LogManager.GetCurrentClassLogger();

        public static bool MainWindowLoaded;
        static readonly bool WaitForDebugger = false;

        public static readonly bool MandatoryUpdate = false;

        public static WriteOnce<bool> FirstRun = new WriteOnce<bool>();

        public static string[] Arguments { get; private set; }
        public static string ArgumentsStr { get; private set; } = "";

        public static bool ClearLogs { get; private set; } = false;
        public static bool NoClient { get; private set; } = false;
        public static bool PrintProcessModules { get; private set; } = false;
        public static bool ExitBeforeMainWindow { get; private set; } = false;
        
        public static string RestartReasonArg { get; private set; } = null;
        public static string UpdateFolderPath { get; private set; } = null;

        public enum RestartReason
        {
            Configuration,
            SelfElevation,
            None
        }
        
        public static RestartReason RestartReasonParsed { get; private set; } = RestartReason.None;

        [STAThread]
        static void Main(string[] tmp_args)
        {
            var args = tmp_args.Distinct().ToArray();
            Arguments = args;
            if (args.Length > 0)
            {
                NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                if (!HasConsole()) NativeMethods.AllocConsole();
            }
            else if (DebugBuild) ConsoleState(true);
            var showHelp = false;
            List<string> extraArgs;
            var optionSet = new OptionSet()
            {
                { Strings.noClientArg, "Allow boosting game while client is not running.", v => { NoClient = v != null; } },
                { Strings.printProcessModulesArg, "Log process modules.", v => { PrintProcessModules = v != null; } },
                { Strings.clearLogsArg, "Clear logs folder on startup.", v => { ClearLogs = v != null; } },
                { Strings.RestartReasonArg, "Reason why program had restarted. {}", v => { RestartReasonArg = v; } },
                { Strings.UpdateFolderArg, "{Path} for the update folder.", v => { UpdateFolderPath = v; } },
                { Strings.ExitBeforeMainWindow, "Terminate program before launching main window.", v => { ExitBeforeMainWindow = v != null; } },
                { Strings.helpArg, "Show this message and exit.", v => { showHelp = v != null; } }
            };
            
            try
            {
                extraArgs = optionSet.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("LeagueFPSBoost: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `leaguefpsboost --help` for more information.");
                return;
            }
            if (showHelp)
            {
                ShowHelp(optionSet);
                return;
            }

            if (RestartReasonArg != null)
                if (RestartReasonArg == RestartReason.Configuration.ToString())
                {
                    RestartReasonParsed = RestartReason.Configuration;
                    PreNLog("Program has restarted itself because there was an error with configuration.");
                }
                else if (RestartReasonArg == RestartReason.SelfElevation.ToString())
                {
                    RestartReasonParsed = RestartReason.SelfElevation;
                    PreNLog("Program has restarted itself with admin rights.");
                }

            foreach (string s in Arguments) ArgumentsStr += s + " ";
            if (WaitForDebugger)
            {
                while (!Debugger.IsAttached) { Thread.Sleep(100); }
                Debugger.Break();
            }
            PreNLog($"Starting LeagueFPSBoost {CurrentVersionFull}");
            PrenlogCmdArgs(args);
            Init();
            PreNLog("Enabling Visual Styles.");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Start recording all user input actions
            ER.Report.StartInputLogging(10,true);
            // Start capturing performance data
            ER.Report.StartPerformanceLogging(60);

            if (!SelfElevation.Elevate()) { return; }
            PreNLog("Current process is in role administrator.");

            if(!CheckFrameworkVer())
            {
                if(DialogResult.Yes == MessageBox.Show("This program cannot run without .NET Framework 4.7.2 installed. Do you want to download it?", "LeagueFPSBoost: Cannot find .NET Framework version", MessageBoxButtons.YesNo, MessageBoxIcon.Error))
                {
                    
                    PreNLog("Trying to open .net framework url: " + Strings.DOT_NET_FRAMEWORK_URL);
                    if (OpenUrl.Open(Strings.DOT_NET_FRAMEWORK_URL))
                    {
                        PreNLog("Success!");
                    }
                    else
                    {
                        PreNLog("Opening .net framework url failed. If this machine is running windows 7 it can be machine specific problem. Try downloading .NET Framework VER>4.7.2 manually.");
                    }

                }
                MessageBox.Show("Program will now exit!", "LeagueFPSBoost: Cannot find .NET Framework version", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DeleteTempUpdaterFilesAndCreateUpdateFolder(UpdateFolderPath);

            CodeStep = 1; // LeagueFPSBoost: Fatal Error While Checking Mutex
            PreNLog("Checking if program is already open.");
            if (!Mutex.WaitOne(TimeSpan.Zero, true))
            {
                PreNLog("Program is already running. Closing this instance after message box.");
                MessageBox.Show("Check task bar, application is already open." + Environment.NewLine + "Program will now exit.", "LeagueFPSBoost: Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PreNLog("This is only instance of LeagueFPSBoost.");

            PreNLog("Checking for program's filename."); // Must be "LeagueFPSBoost.exe" becuase of updater
            if(!Assembly.GetEntryAssembly().Location.EndsWith("LeagueFPSBoost.exe", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    PreNLog("Trying to check if LeagueFPSBoost.exe exists in this directory.");
                    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "LeagueFPSBoost.exe");
                    if (File.Exists(path))
                    {
                        PreNLog("Launching correctly named file.");
                        Restart.RestartNow(path);
                    }
                }
                catch(Exception ex)
                {
                    PreNLog("Failed: " + Environment.NewLine + ex);
                }
                
                PreNLog("Program's filename is incorrect, updater will fail to relaunch program. Please rename the program executable to 'LeagueFPSBoost'.");
                MessageBox.Show("Program's filename is incorrect, updates will fail to relaunch program. Please rename the program executable to 'LeagueFPSBoost'. If you don't rename program's file you cant' run it. Program will exit after clicking on 'OK'.", "LeagueFPSBoost: Wrong Filename", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            PreNLog("Program's filename is correct.");

            CodeStep = 2; // LeagueFPSBoost: Fatal Error While Finding League Folder Path
            if (!FindPath())
            {
                MessageBox.Show("Cant find LoL folder path.", "LeagueFPSBoost: Fatal Error While Finding League Folder Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // logger enabled

            if (ExitBeforeMainWindow)
            {
                Logger.Info("Exit argument has been specified. Exiting before checking for league's config.");
                Environment.Exit(0);
            }

            if (!CheckForLeagueConfig())
            {
                Logger.Fatal("League's game.cfg file is missing. Path: " + Path.Combine(LeagueConfigDirPath, @"game.cfg"));
                MessageBox.Show("This program cannot run without configuration file(game.cfg).", "LeagueFPSBoost: Missing LoL configuration file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Startup();

            Logger.Debug("ServicePointManager.SecurityProtocol = " + ServicePointManager.SecurityProtocol);
            Logger.Debug("Setting security protocols.");
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)48 | (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            Logger.Debug("ServicePointManager.SecurityProtocol = " + ServicePointManager.SecurityProtocol);

            HideConsole(Arguments);
            using (var mainWindow = new MainWindow())
            {
                if (FirstRun.Value)
                {
                    MessageBox.Show("If you like this program please share it with your friends." + Environment.NewLine +
                                    "Also feedback on any errors/bugs is really useful. Visit the" + Environment.NewLine +
                                    "boards page and find GitHub repository link and submit new " + Environment.NewLine +
                                    "issue and I will try to fix it. Boards link can be found in" + Environment.NewLine +
                                    "about tab. Good luck! - IShunpoYourFace (EUNE) July 2018", $"Welcome to LeagueFPSBoost version {Program.CurrentVersionFull}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                Application.Run(mainWindow);
            }
            Logger.Debug("MainWindow closed.");

            Dispose();
            Logger.Debug("Shutting down program.");
        }

        private static bool CheckFrameworkVer()
        {
            PreNLog("Checking for framework version 4.7.2 or later.");
            try
            {
                if (Framework.Net472OrLaterInstalled())
                {
                    PreNLog("Version 4.7.2 or later is found!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                PreNLog(Strings.exceptionThrown + " while checking for framework version: " + Environment.NewLine + ex);
            }
            PreNLog("Couldn't find required framework version.");
            return false;
        }

        private static void DeleteTempUpdaterFilesAndCreateUpdateFolder(string folderPath)
        {
            var dir = "";
            var zipFileName = "LeagueFPSBoost.zip";
            var zipExtractorName = "ZipExtractor.exe";
            var fileToCompress = Assembly.GetExecutingAssembly().Location;
            var downloadedZip = Path.Combine(Directory.GetCurrentDirectory(), zipFileName);
            var zipExtractorPath = Path.Combine(Directory.GetCurrentDirectory(), zipExtractorName);
            
            for(int i = 0; i < 5; i++)
            {
                try
                {
                    PreNLog("Trying to delete temporary files.");
                    if (File.Exists(downloadedZip))
                    {
                        File.Delete(downloadedZip);
                        PreNLog("Old temporary update file is found and deleted: " + downloadedZip);
                    }
                    if (File.Exists(zipExtractorPath))
                    {
                        File.Delete(zipExtractorPath);
                        PreNLog("Old temporary zip extractor file is found and deleted: " + zipExtractorPath);
                    }
                    PreNLog("Done.");
                    break;
                }
                catch (Exception ex)
                {
                    PreNLog(Strings.exceptionThrown + " while trying to delete temporary updater's files: " + Environment.NewLine + ex);
                    PreNLog("Trying again...");
                }
            }

            var PathSpecified = !string.IsNullOrWhiteSpace(folderPath);
            if (PathSpecified)
            {
                PreNLog("Update directory path has been specified.");

                
                var validChk = false;
                for(int i = 0; i < 5; i++)
                {
                    try
                    {
                        PreNLog("Checking if specified path is valid.");
                        if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
                        Directory.CreateDirectory(folderPath);
                        validChk = Directory.Exists(folderPath);
                        if (validChk) break;
                    }
                    catch (Exception ex)
                    {
                        PreNLog(Strings.exceptionThrown + " while trying to check if specified update directory path is valid: " + Environment.NewLine + ex);
                        PreNLog("Trying again...");
                    }
                }

                if (validChk)
                {
                    dir = Path.Combine(folderPath);
                    PreNLog("Specified update directory path is valid: " + dir);
                }
                else
                {
                    dir = Path.Combine(Environment.CurrentDirectory, "Update");
                    PreNLog("Specified update directory path is not valid. Using default one: " + dir);
                }


                var zipFilePath = Path.Combine(dir, zipFileName);
                var xmlFilePath = Path.Combine(dir, "updater.xml");
                var jsonFilePath = Path.Combine(dir, "updater.json");


                
                for(int i = 0; i < 5; i++)
                {
                    try
                    {
                        PreNLog("Trying to create update folder.");
                        if (Directory.Exists(dir)) Directory.Delete(dir, true);
                        Directory.CreateDirectory(dir);

                        using (var newZipFile = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                        {
                            newZipFile.CreateEntryFromFile(fileToCompress, Path.GetFileName(fileToCompress));
                        }

                        PreNLog("Created update zip file: " + zipFilePath);

                        var checksum = new Checksum();

                        using (var fs = File.OpenRead(zipFilePath))
                        {
                            checksum = new Checksum(fs, ChecksumType.SHA512);
                        }
                        
                        var xmlUpdaterData = new UpdaterData(xmlFilePath, UpdaterDataTypeFormat.XDocument, checksum, MandatoryUpdate);

                        xmlUpdaterData.Save();

                        PreNLog("Created update xml file: " + xmlFilePath);

                        var jsonUpdaterData = new UpdaterData(jsonFilePath, UpdaterDataTypeFormat.JavaScriptObjectNotation, checksum, MandatoryUpdate);
                        jsonUpdaterData.AddMessageBox(MessageBoxList.GameBarAndFullScrOptim);

                        if (jsonUpdaterData.Save())
                            PreNLog("Created update json file: " + jsonFilePath);
                        else
                            PreNLog("Couldn't create json file: " + jsonFilePath);

                        if (!DebugBuild)
                        {
                            MessageBox.Show("Update folder has been created.", "LeagueFPSBoost");
                        }
                        PreNLog("Update folder has been created!");
                        if (ExitBeforeMainWindow)
                        {
                            Logger.Info("Exit argument has been specified. Exiting before checking for league's config.");
                            Environment.Exit(0);
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        PreNLog(Strings.exceptionThrown + " while deleting temporary files or creating update folder: " + Environment.NewLine + ex);
                        PreNLog("Trying again...");
                    }
                }
            }
        }

        static void PrenlogCmdArgs(string[] args)
        {
            var cmdArgsSB = new StringBuilder();
            cmdArgsSB.Append("Command line arguments:");
            if (args.Length != 0) cmdArgsSB.AppendLine();
            foreach (string arg in args)
            {
                cmdArgsSB.Append(Strings.tab).Append(arg).AppendLine();
            }
            if (cmdArgsSB[cmdArgsSB.Length - 1] == '\n') cmdArgsSB.Remove(cmdArgsSB.Length - 1, 1);
            PreNLog(cmdArgsSB.ToString());
        }

        public static void PreNLog(string msg)
        {
            PreNLogMessages.Add(msg);
            Console.WriteLine(msg);
        }

        static void ConfigureNLogger(string folderPath)
        {
            PreNLog("Configuring NLog.");
            // Step 1.Create configuration object
            var config = new LoggingConfiguration();
            PreNLog("NLog's folder is: " + folderPath);
            // Step 2. Create targets
            var fileTarget = new FileTarget("logFile")
            {
                //FileName = folderPath + startTime.ToString(Strings.logDateTimeFormat) + "_${processid}_${processname}.txt",
                FileName = Path.Combine(folderPath, "LeagueFPSBoostNLog.log"),
                Layout = "${longdate}|${level:upperCase=true}|${logger:shortName=false}| ${message} ${exception:format=toString}",
                ArchiveFileName = Path.Combine(folderPath, "archives/{#}.zip"),
                ArchiveDateFormat = Strings.logDateTimeFormat,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 30,
                EnableArchiveFileCompression = true,
            };
            var asyncWrapperFileTarget = new AsyncTargetWrapper(fileTarget)
            {
                Name = "AsyncLogFile",
                TimeToSleepBetweenBatches = 0,
                QueueLimit = 10000,
                OverflowAction = AsyncTargetWrapperOverflowAction.Grow
            };

            config.AddTarget(asyncWrapperFileTarget);
            PreNLog("Async NLog's File Target has been added: " + Environment.NewLine +
                                Strings.tabWithLine + "Async Wrapper Target Name: " + asyncWrapperFileTarget.Name + Environment.NewLine +
                                Strings.doubleTabWithLine + "QueueLimit: " + asyncWrapperFileTarget.QueueLimit + Environment.NewLine +
                                Strings.doubleTabWithLine + "TimeToSleepBetweenBatches: " + asyncWrapperFileTarget.TimeToSleepBetweenBatches + Environment.NewLine +
                                Strings.doubleTabWithLine + "AsyncTargetWrapperOverflowAction: " + asyncWrapperFileTarget.OverflowAction + Environment.NewLine +
                                Strings.doubleTabWithLine + "File Target Name: " + fileTarget.Name + Environment.NewLine +
                                Strings.tripleTabWithLine + "FileName: " + fileTarget.FileName + Environment.NewLine +
                                Strings.tripleTabWithLine + "Layout: " + fileTarget.Layout + Environment.NewLine +
                                Strings.tripleTabWithLine + "ArchiveFileName: " + fileTarget.ArchiveFileName + Environment.NewLine +
                                Strings.tripleTabWithLine + "ArchiveNumbering: " + fileTarget.ArchiveNumbering + Environment.NewLine +
                                Strings.tripleTabWithLine + "ArchiveOldFileOnStartup: " + fileTarget.ArchiveOldFileOnStartup + Environment.NewLine +
                                Strings.tripleTabWithLine + "MaxArchiveFiles: " + fileTarget.MaxArchiveFiles + Environment.NewLine +
                                Strings.tripleTabWithLine + "EnableArchiveFileCompression: " + fileTarget.EnableArchiveFileCompression);

            var traceTarget = new TraceTarget("traceTarget")
            {
                Layout = fileTarget.Layout,
                RawWrite = true
            };
            var asyncWrapperTraceTarget = new AsyncTargetWrapper(traceTarget)
            {
                Name = "AsyncTraceTarget",
                QueueLimit = asyncWrapperFileTarget.QueueLimit,
                TimeToSleepBetweenBatches = asyncWrapperFileTarget.TimeToSleepBetweenBatches,
                OverflowAction = asyncWrapperFileTarget.OverflowAction
            };

            config.AddTarget(asyncWrapperTraceTarget);
            PreNLog("Async NLog's Trace Target has been added: " + Environment.NewLine +
                                Strings.tabWithLine + "Async Wrapper Target Name: " + asyncWrapperTraceTarget.Name + Environment.NewLine +
                                Strings.doubleTabWithLine + "QueueLimit: " + asyncWrapperTraceTarget.QueueLimit + Environment.NewLine +
                                Strings.doubleTabWithLine + "TimeToSleepBetweenBatches: " + asyncWrapperTraceTarget.TimeToSleepBetweenBatches + Environment.NewLine +
                                Strings.doubleTabWithLine + "AsyncTargetWrapperOverflowAction: " + asyncWrapperTraceTarget.OverflowAction + Environment.NewLine +
                                Strings.doubleTabWithLine + "Trace Target Name: " + traceTarget.Name + Environment.NewLine +
                                Strings.tripleTabWithLine + "Layout: " + traceTarget.Layout + Environment.NewLine +
                                Strings.tripleTabWithLine + "RawWrite: " + traceTarget.RawWrite);

            var coloredConsoleTarget = new ColoredConsoleTarget("colorConsoleTarget")
            {
                Layout = fileTarget.Layout,
                DetectConsoleAvailable = false
            };
            var asyncWrapperColoredConsoleTarget = new AsyncTargetWrapper(coloredConsoleTarget)
            {
                Name = "AsyncColoredConsoleTarget",
                QueueLimit = asyncWrapperFileTarget.QueueLimit,
                TimeToSleepBetweenBatches = asyncWrapperFileTarget.TimeToSleepBetweenBatches,
                OverflowAction = asyncWrapperFileTarget.OverflowAction
            };

            config.AddTarget(asyncWrapperColoredConsoleTarget);
            PreNLog("Async NLog's Colored Console Target has been added: " + Environment.NewLine +
                                Strings.tabWithLine + "Async Wrapper Target Name: " + asyncWrapperColoredConsoleTarget.Name + Environment.NewLine +
                                Strings.doubleTabWithLine + "QueueLimit: " + asyncWrapperColoredConsoleTarget.QueueLimit + Environment.NewLine +
                                Strings.doubleTabWithLine + "TimeToSleepBetweenBatches: " + asyncWrapperColoredConsoleTarget.TimeToSleepBetweenBatches + Environment.NewLine +
                                Strings.doubleTabWithLine + "AsyncTargetWrapperOverflowAction: " + asyncWrapperColoredConsoleTarget.OverflowAction + Environment.NewLine +
                                Strings.doubleTabWithLine + "Trace Target Name: " + coloredConsoleTarget.Name + Environment.NewLine +
                                Strings.tripleTabWithLine + "Layout: " + coloredConsoleTarget.Layout);

            // Step 3. Define rules
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncWrapperFileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncWrapperTraceTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncWrapperColoredConsoleTarget);

            // Step 4. Activate the configuration
            LogManager.Configuration = config;
            LogManager.ReconfigExistingLoggers();
            PreNLog("NLogger enabled!");

            Logger.Info("Printing info that happened before logger has been initialized.");
            foreach (string s in PreNLogMessages)
            {
                Logger.Info(s);
            }
            Logger.Info("End of printing info that happened before logger has been initialized.");
            Logger.Debug("Logger is fully initialized.");
            var task = LogSOFT_HARD_Info(Logger);
        }

        public static async Task LogSOFT_HARD_Info(Logger log)
        {
            var task = new Task<string>(GetSoftwareAndHardwareInfo);
            log.Trace("Reading system, hardware and software information.");
            task.Start();
            var logTxt = await task;
            log.Debug("Finished reading system and hardware and software information task: " + Environment.NewLine + logTxt);
        }

        public static string GetSoftwareAndHardwareInfo()
        {
            try
            {
                return ReadOS_CPU_GPU_Info();
            }
            catch(Exception ex)
            {
                return Strings.exceptionThrown + " while trying to ReadOS_CPU_GPU_Info: " + Environment.NewLine + ex;
            }
        }

        static string ReadOS_CPU_GPU_Info()
        {
            PiSB.Append(Process.GetCurrentProcess().GetProcessInfoForLogging(PrintProcessModules));
            Logger.Debug("Started reading Win32_OperatingSystem.");
            var mos = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject mo in mos.Get())
            {
                OsSB.AppendLine(Strings.tabWithLine + "Computer name: " + mo["CSName"]);
                OsSB.AppendLine(Strings.tabWithLine + "Operating system: ");
                OsSB.AppendLine(Strings.doubleTabWithLine + "Caption: " + mo["Caption"]);
                OsSB.AppendLine(Strings.doubleTabWithLine + "Architecture: " + mo["OSArchitecture"]);
                OsSB.AppendLine(Strings.doubleTabWithLine + "Version: " + mo["Version"]);
                OsSB.AppendLine(Strings.doubleTabWithLine + "Build number: " + mo["BuildNumber"]);
                OsSB.AppendLine(Strings.doubleTabWithLine + "Build type: " + mo["BuildType"]);
                OsSB.AppendLine(Strings.doubleTabWithLine + "Manufacturer: " + mo["Manufacturer"]);
                try
                {
                    var totalMemory = ByteSize.FromKiloBytes(Convert.ToDouble(mo["TotalVisibleMemorySize"]));
                    var freeMemory = ByteSize.FromKiloBytes(Convert.ToDouble(mo["FreePhysicalMemory"]));
                    var usedMemory = totalMemory - freeMemory;
                    OsSB.AppendLine(Strings.doubleTabWithLine + "Total Memory: " + totalMemory.ToString("##.## GB"));
                    OsSB.AppendLine(Strings.doubleTabWithLine + "Used Memory: " + usedMemory.ToString("##.## GB"));
                    OsSB.AppendLine(Strings.doubleTabWithLine + "Free Memory: " + freeMemory.ToString("##.## GB"));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, Strings.exceptionThrown + " while reading system memory." + Environment.NewLine);
                }
                OsSB.AppendLine(Strings.doubleTabWithLine + "Product suite: " + mo["OSProductsuite"]);
                OsSB.Append(Strings.doubleTabWithLine + "Type: " + mo["OSType"]);
            }
            Logger.Debug("Finished reading Win32_OperatingSystem.");

            Logger.Debug("Started reading Win32_Processor.");
            mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject mo in mos.Get())
            {
                CpuSB.AppendLine(Strings.tabWithLine + mo["DeviceID"] + ":");
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Name: " + mo["Name"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Address width: " + mo["AddressWidth"] + " Bit");
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Data width: " + mo["DataWidth"] + " Bit");
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Cores: " + mo["NumberOfCores"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Enabled cores: " + mo["NumberOfEnabledCore"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Logical Processors: " + mo["NumberOfLogicalProcessors"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Max clock speed: " + mo["MaxClockSpeed"] + " MHz");
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Current clock speed: " + mo["CurrentClockSpeed"] + " MHz");
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Manufacturer: " + mo["Manufacturer"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Socket: " + mo["SocketDesignation"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "Virtualization: " + mo["VirtualizationFirmwareEnabled"]);
                CpuSB.AppendLine(Strings.doubleTabWithLine + "VM Monitor Mode Extensions: " + mo["VMMonitorModeExtensions"]);
            }
            if (CpuSB[CpuSB.Length - 1] == '\n') CpuSB.Remove(CpuSB.Length - 1, 1);
            Logger.Debug("Finished reading Win32_Processor.");

            Logger.Debug("Started reading Win32_VideoController.");
            mos = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject mo in mos.Get())
            {
                GpuSB.AppendLine(Strings.tabWithLine + mo["DeviceID"] + ":");
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Name: " + mo["Name"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Caption: " + mo["Caption"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Processor: " + mo["VideoProcessor"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Adapter compatibility : " + mo["AdapterCompatibility"]);
                try
                {

                    GpuSB.AppendLine(Strings.doubleTabWithLine + "Adapter RAM: " + ByteSize.FromBytes(Convert.ToInt64(mo["AdapterRAM"])).ToString("##.## MB"));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, Strings.exceptionThrown + " while reading video controller memory." + Environment.NewLine);
                }
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Video mode description: " + mo["VideoModeDescription"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Min refresh rate: " + mo["MinRefreshRate"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Max refresh rate: " + mo["MaxRefreshRate"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Current horizontal resolution: " + mo["CurrentHorizontalResolution"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Current vertical resolution: " + mo["CurrentVerticalResolution"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Current refresh rate: " + mo["CurrentRefreshRate"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Current number of colors: " + mo["CurrentNumberOfColors"]);
                GpuSB.AppendLine(Strings.doubleTabWithLine + "Video architecture : " + mo["VideoArchitecture"]);
            }
            if (GpuSB[GpuSB.Length - 1] == '\n') GpuSB.Remove(GpuSB.Length - 1, 1);
            Logger.Debug("Finished reading Win32_VideoController.");

            var sb = new StringBuilder();
            sb.AppendLine("System information:");
            sb.AppendLine(OsSB.ToString());
            sb.AppendLine("Processor information:");
            sb.AppendLine(CpuSB.ToString());
            sb.AppendLine("Video controller information:");
            sb.AppendLine(GpuSB.ToString());
            sb.AppendLine("Current process information:");
            sb.AppendLine(PiSB.ToString());
            sb.AppendLine("LeagueFPSBoost information:");
            sb.AppendLine(GetLeagueFPSBoostInformation());
            sb.AppendLine("League client information:");
            sb.AppendLine(GetLeagueClientInformation(Path.Combine(LeaguePath, "LeagueClient.exe")));
            
            return sb.ToString();
        }

        static void Init()
        {
            
            PreNLog("Initializing Application events.");
            Application.ThreadException += Application_ThreadException;
            Application.ApplicationExit += OnApplicationExit;

            PreNLog("Setting Application UnhandledExceptionMode to CatchException");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            PreNLog("Initializing AppDomain UnhandledException Event.");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //throw new Exception();
            PreNLog("Initializing LeaguePriority events.");
            LeaguePriority.GameBoostOk += OnGameBoostOk;
            LeaguePriority.GameBoostFail += OnGameBoostFail;
            LeaguePriority.ClientNormalFail += OnClientNormalFail;
            LeaguePriority.ClientNormalOk += OnClientNormalOk;

            PreNLog("Initializing mutex.");
            Mutex = new Mutex(true, @"{63163300-b738-45b6-936f-3b1334617004}");

            Logger = LogManager.GetCurrentClassLogger();
        }

        static void Dispose()
        {
            Logger.Debug("Disposing program.");


            try
            {
                Logger.Debug($"Returning power plan to last used one: {MainWindow.currentLastActivePowerPlan} - {PowerManager.GetPlanName(MainWindow.currentLastActivePowerPlan)}");
                PowerManager.SetActivePlan(MainWindow.currentLastActivePowerPlan);
                Logger.Debug("Change was successful without exceptions.");
            }
            catch(Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while changing power plan to last used one: " + MainWindow.currentLastActivePowerPlan + Environment.NewLine);
            }

            try
            {
                Logger.Debug("Stopping process start wmi watcher.");
                StartWatch.Stop();
                Logger.Debug("Stopping process stop wmi watcher.");
                StopWatch.Stop();

                Logger.Debug("Disposing process start wmi watcher.");
                StartWatch.Dispose();
                Logger.Debug("Disposing process stop wmi watcher.");
                StopWatch.Dispose();
            }
            catch (COMException ex)
            {
                Logger.Warn(ex, Strings.exceptionThrown + " while trying to stop or dispose ManagementEventWatcher. Can happen if the user has already disconnected. Ignoring and continuing shutting down. " + Environment.NewLine);
            }

            try
            {
                Logger.Debug("Trying to release mutex.");
                Mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while trying to release mutex." + Environment.NewLine);
            }
        }

        static void Startup()
        {


            CodeStep = 4; // LeagueFPSBoost: Error While Starting Logging System
            StartLogger();

            StartWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            StartWatch.EventArrived += ProcessEvents.StartCheckWatch_EventArrived;
            Logger.Trace("Subscribed to Win32_ProcessStartTrace event.");

            StopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            StopWatch.EventArrived += ProcessEvents.StopCheckWatch_EventArrived;
            Logger.Trace("Subscribed to Win32_ProcessStopTrace event.");
        }

        static bool CheckForLeagueConfig()
        {
            return File.Exists(Path.Combine(LeagueConfigDirPath, @"game.cfg"));
        }

        static void OnClientNormalOk(object sender, LeagueBoostEventArgs e)
        {
            LeagueLogger.Info("Client Returning To Normal Succeeded.");
        }

        static void OnClientNormalFail(object sender, LeagueBoostErrorEventArgs e)
        {
            LeagueLogger.Error("Client Returning To Normal Failed: " + e.Exception.Message);
        }

        static void OnGameBoostFail(object sender, LeagueBoostErrorEventArgs e)
        {
            if (e.ClientRunning)
            {
                LeagueLogger.Error("Game Boosting Failed: " + e.Exception.Message);
            }
            else
            {
                LeagueLogger.Error("Game Boosting Failed: " + e.Reason + " " + e.Exception.Message);
            }
            PlayNoti(false);
        }

        static void OnGameBoostOk(object sender, LeagueBoostEventArgs e)
        {
            LeagueLogger.Info("Game Boosting Succeeded.");
            PlayNoti(true);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LeagueLogger.Error("Application Thread Exception Occurred!");
            try
            {
                Logger.Fatal(e.Exception, "Application thread exception has been thrown: " + Environment.NewLine);
                ReportCrash(e.Exception);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while creating/sending crash report: " + Environment.NewLine);
                MessageBox.Show("Unknown error: \n" + ex, "LeagueFPSBoost: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Logger.Debug("Exiting with code: " + e.Exception.HResult);
            MessageBox.Show("Program will now exit. Error code: " + e.Exception.HResult + " ( 0x" + e.Exception.HResult.ToString("X") + " )", "LeagueFPSBoost: ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(e.Exception.HResult);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LeagueLogger.Error("Current Domain Unhandled Exception Occurred!");
            try
            {
                Logger.Fatal((Exception)e.ExceptionObject, "Current domain unhandled exception has been thrown: " + Environment.NewLine);
                ReportCrash((Exception)e.ExceptionObject);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, Strings.exceptionThrown + " while creating/sending crash report: " + Environment.NewLine);
                MessageBox.Show("Unknown error: \n" + ex, "LeagueFPSBoost: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            throw (Exception)e.ExceptionObject;
        }

        public static void ReportCrash(Exception exception)
        {
            try
            {
                Thread thread = new Thread(() =>
                {
                    var progressForm = new ProgressBarWindow();
                    if (!Application.MessageLoop)
                        Application.Run(progressForm);
                    else
                        progressForm.Show();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while opening progress bar form: " + Environment.NewLine);
            }
            
            Logger.Debug("Building crash string for crash report.");
            try
            {
                CrashSb.AppendLine("PreLogWarnings: ");
                CrashSb.AppendLine(string.Join(Environment.NewLine, PreLogWarnings.ToArray()) + Environment.NewLine + "End of pre log warnings.");

                CrashSb.AppendLine();

                CrashSb.Append("League path var: ").AppendLine(LeaguePath);
                CrashSb.Append("League configuration directory path var: ").AppendLine(LeagueConfigDirPath);
                CrashSb.Append("League log directory path var: ").AppendLine(LeagueLogFileDirPath);
                CrashSb.Append("League game path: ").AppendLine(LeagueGamePath);
                CrashSb.Append("OS Version: " ).AppendLine(Environment.OSVersion.Version.ToString());

                CrashSb.AppendLine();

                Logger.Error("A crash has been detected: " + Environment.NewLine + Environment.NewLine + exception + Environment.NewLine);
                Logger.Info("Developer message: " + Environment.NewLine + Environment.NewLine + CrashSb);
                Logger.Debug("Starting CrashReporter.Net");

                ReportCrash2(exception, CrashSb.ToString());
                Logger.Debug("Crash report dialog has been closed.");
                Logger.Warn("Program will now terminate.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while building crash string for crash report: " + Environment.NewLine);
            }
        }

        public static void OnApplicationExit(object sender, EventArgs e)
        {
            Logger.Info("Exiting application.");
            LeagueLogger.Always("Exiting program.");
        }

        public static void ReportCrash2(Exception exception, string developerMessage = "")
        {
            Logger.Debug("Showing crash report message box.");
            MessageBox.Show("Application has crashed. Error report will be sent to developer right now so that he can fix this issue easier. Please wait minute or two. You will be notified when its finished.", "LeagueFPSBoost Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            var contactInfo = Microsoft.VisualBasic.Interaction.InputBox("Please enter your contact information like email address or summoner name and region... so that developer can contact you. Developer's contact email is leaguefpsboost@gmail.com", "Crash contact information", "", -1, -1);
            Logger.Debug("Creating crash report using CrashReporter.Net");
            Logger.Debug("Contact info: " + contactInfo);
            developerMessage += Environment.NewLine + "Contact: " + (string.IsNullOrEmpty(contactInfo) ? "Not provided" : contactInfo) + Environment.NewLine;
            
            var tmpcrashsb = new StringBuilder();
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(20, "Creating 1st report.."));
            tmpcrashsb.AppendLine();
            tmpcrashsb.Append("Log: " + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, PreNLogMessages.ToArray()) + Environment.NewLine + "End of pre nlog messages.");
            tmpcrashsb.AppendLine();

            tmpcrashsb.AppendLine();
            developerMessage += tmpcrashsb.ToString();

            var reportCrash = new ReportCrash(StringCipher.Decrypt(
                                                            DeveloperData.CrashReport_cipherText,
                                                            DeveloperData.CrashReport_passPharse,
                                                            DeveloperData.CrashReport_keySize))
            {
                DeveloperMessage = developerMessage,
                CaptureScreen = true,
                EmailRequired = false,
                IncludeScreenshot = true,
                Silent = true
            };
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(30, "Creating 2st report.."));
            var sb = new StringBuilder();
            sb.AppendLine(Strings.tabWithLine + "Crash report:");
            sb.AppendLine(Strings.doubleTabWithLine + "Capture screen: " + reportCrash.CaptureScreen);
            sb.AppendLine(Strings.doubleTabWithLine + "Email required: " + reportCrash.EmailRequired);
            sb.Append(Strings.doubleTabWithLine + "Include screenshot: " + reportCrash.IncludeScreenshot);
            Logger.Debug("Crash report has been created: " + Environment.NewLine + sb);

            Logger.Debug("Creating crash report using Exception Reporters.");
            var FilesToEmail = new List<string>();
            var htmlReportPath = HelpingExtensions.GetTempFilePath(".html", "crashInfo");
            var additionalInfoFilePath = HelpingExtensions.GetTempFilePath(".txt", "info");
            var errorReport = ER.Report.GetErrorReport(exception);
            var zipFileName = string.Empty;
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(40, "Archiving files for 2nd report.."));
            try
            {
                if (!string.IsNullOrEmpty(LeagueLogFileDirPath) && Directory.Exists(LeagueLogFileDirPath))
                {
                    htmlReportPath = Path.Combine(LeagueLogFileDirPath, "MainLog").GetTempFilePath(".html", "crashInfo");
                    FilesToEmail.AddRange(Directory.GetFiles(LeagueLogFileDirPath, "*.*", SearchOption.AllDirectories).Where(name => !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList());
                }
                File.WriteAllText(htmlReportPath, errorReport.Html);
                File.WriteAllText(additionalInfoFilePath, "If there is only html file in this report then LeagueLogFileDirPath is null or empty or it does not exist.");
                FilesToEmail.Add(htmlReportPath);
                FilesToEmail.Add(additionalInfoFilePath);
                Logger.Debug("Successfully created crash reports.");
                OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(50, "Done archiving files.."));
            }
            catch (Exception exc)
            {
                Logger.Error(exc, Strings.exceptionThrown + " while creating files to attach list: " + Environment.NewLine);
            }
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(60, "Sending 1st report.."));
            Logger.Debug("Sending silent CrashReporter.NET crash report.");
            try
            {
                reportCrash.SendSilently(exception);
                Logger.Debug("Successfully sent CrashReporter.NET crash report.");
                OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(65, "1st report sent.."));
            }
            catch (Exception excreportCrash)
            {
                Logger.Error(excreportCrash, Strings.exceptionThrown + " while sending CrashReporter.NET crash report: " + Environment.NewLine);
            }

            Logger.Debug("Creating ExceptionReporter.NET crash report for sending via mail.");
            try
            {
                OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(70, "Sending 2nd report.."));
                var er = new ExceptionReporter
                {
                    Config =
                    {
                        AppName = "LeagueFPSBoost",
                        CompanyName = "http://youtube.com/+ncky",
                        TitleText = "LeagueFPSBoost Error Report",
                        FilesToAttach = FilesToEmail.ToArray(),
                        ReportTemplateFormat = TemplateFormat.Markdown,
                        EmailReportAddress = StringCipher.Decrypt(DeveloperData.CrashReport_cipherText, DeveloperData.CrashReport_passPharse, DeveloperData.CrashReport_keySize),

                        SendMethod = ReportSendMethod.SMTP,
                        SmtpServer = "smtp.gmail.com",
                        SmtpPort = 587,
                        SmtpUsername = StringCipher.Decrypt(DeveloperData.ExceptionReporter_chipterText1, DeveloperData.ExceptionReporter_passPharse1, DeveloperData.ExceptionReporter_keySize1),
                        SmtpPassword = StringCipher.Decrypt(DeveloperData.ExceptionReporter_chipterText2, DeveloperData.ExceptionReporter_passPharse2, DeveloperData.ExceptionReporter_keySize2),
                        SmtpFromAddress = StringCipher.Decrypt(DeveloperData.ExceptionReporter_chipterText1, DeveloperData.ExceptionReporter_passPharse1, DeveloperData.ExceptionReporter_keySize1),
                        SmtpUseSsl = true,



                        ShowAssembliesTab = true,
                        ShowEmailButton = true,
                        ShowGeneralTab = true,
                        ShowSysInfoTab = true,
                        ShowExceptionsTab = true,
                        TakeScreenshot = true,
                        TopMost = true,
                    }
                };

                Logger.Debug("Created ExceptionReporter.NET crash report. Sending now...");

                er.Send(new ExceptionReporterSendEvent(), exception);

                /*var er2 = new ExceptionReporter();
                Logger.Debug("Showing ExceptionReporter.NET dialog.");
                er2.Show(exception);*/
            }
            catch (Exception excExcReport)
            {
                Logger.Error(excExcReport, Strings.exceptionThrown + " while sending ExceptionReporter.NET crash report: " + Environment.NewLine);
            }
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(80, "Opening 2nd report.."));

            Logger.Debug("Showing crash report window.");
            try
            {
                ER.Report.ShowException(null, exception, null);
                Logger.Debug("Crash report window has been closed.");
            }
            catch (Exception excReport)
            {
                Logger.Error(excReport, Strings.exceptionThrown + " while showing crash report window: " + Environment.NewLine);
            }
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(90, "Deleting local report.."));

            Logger.Debug("Deleting current crash report html file: " + htmlReportPath);
            var successChk = false;
            var countChk = 0;
            while (!successChk && ++countChk < 5)
            {
                try
                {
                    if (File.Exists(htmlReportPath))
                    {
                        Logger.Debug("Current crash report file exists. Deleting..");
                        File.Delete(htmlReportPath);
                        Logger.Debug("Deleted: " + htmlReportPath);
                    }
                    else
                    {
                        Logger.Debug("Current crash report file doesn't exist. Skipping deleting.");
                    }
                    successChk = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while deleting current crash report file: " + Environment.NewLine);
                    Logger.Info("Trying again to delete current crash report file.");
                    successChk = true;
                }
            }
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(95, "Local report deleted..."));
            OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(95, "Wait for 2nd report...."));

        }

        class ExceptionReporterSendEvent : IReportSendEvent
        {
            public void Completed(bool success)
            {
                var txt = "Error";
                if (success)
                {
                    txt = "Sending error report via email has finished successfully.";
                    Logger.Debug(txt);
                    OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(100, "2nd report sent.."));
                    Task.Run(() =>
                    {
                        MessageBox.Show(txt, "LeagueFPSBoost Crash Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                else
                {
                    txt = "Sending error report via email has failed.";
                    Logger.Error(txt);
                    OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(100, "2nd report not sent.."));
                    Task.Run(() =>
                    {
                        MessageBox.Show(txt, "LeagueFPSBoost Crash Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                OnCrashUploadReport?.Invoke(null, new CrashUploadReportEventArgs(100, "Done.."));

            }

            public void ShowError(string message, Exception exception)
            {
                var txt = Strings.exceptionThrown + " while sending error report via email: " + Environment.NewLine + "Message: " + message + Environment.NewLine;
                Logger.Error(exception, txt);
                Task.Run(() =>
                {
                    MessageBox.Show(txt + exception, "LeagueFPSBoost Crash Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }


        static void PathMissing(bool showMsgBox)
        {
            PathFound = false;
            if (showMsgBox) MessageBox.Show("Cannot find path for League Of Legends in registry!", "LeagueFPSBoost: Can't Set Game Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void PathMissing(bool showMsgBox, string msg)
        {
            PathFound = false;
            if (showMsgBox) MessageBox.Show(msg, "LeagueFPSBoost: Can't Set Game Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static bool FindPath()
        {
            PreNLog("Trying to find league's root directory path in settings file.");
            try
            {
                var settingsLeaguePath = Settings.Default.LeaguePath;
                PreNLog("Got settingsLeaguePath from settings: " + settingsLeaguePath);
                if (string.IsNullOrEmpty(settingsLeaguePath)) throw new ArgumentException("League's root directory path from settings is empty.");
                PreNLog("Checking if path from settings exists.");
                if (Directory.Exists(settingsLeaguePath))
                {
                    PreNLog("Checking if path from settings is correct.");
                    if (UpdateDirPath(settingsLeaguePath))
                    {
                        PreNLog("Successfully found path in settings: " + settingsLeaguePath);
                        return PathFound;
                    }
                    else
                    {
                        var ex = new FileNotFoundException("LeagueClient.exe & BsSndRpt.exe don't exist in path found in settings.");
                        ex.Data["DirectoryPath"] = settingsLeaguePath;
                        throw ex;
                    }
                }
                else
                {
                    var ex = new DirectoryNotFoundException("League's root directory path from settings does not exist.");
                    ex.Data["DirectoryPath"] = settingsLeaguePath;
                    throw ex;
                }
            }
            catch (Exception exSettings)
            {
                PreNLog(Strings.exceptionThrown + " while trying league's root directory path from settings: " + Environment.NewLine + exSettings);

                PreNLog("Trying to find league's root directory path in registry.");
                try
                {
                    using (var rk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(Strings.LeagueRegistrySubKeyName))
                    {
                        try
                        {
                            if (rk.GetValue(Strings.LeagueRegistryLocationStringName) != null)
                            {
                                var regPath = Path.Combine(rk.GetValue(Strings.LeagueRegistryLocationStringName).ToString());
                                PreNLog("Checking if registry path is correct: " + regPath);
                                if (UpdateDirPath(regPath))
                                {
                                    PreNLog("Successfully found path in registry: " + regPath);
                                    return PathFound;
                                }
                                else
                                {
                                    PreNLog("LeagueClient.exe & BsSndRpt.exe don't exist in path found in registry.");
                                }
                            }
                            else
                            {
                                PreNLog("Couldn't find path in registry: rk.GetValue(LeagueRegistryLocationStringName) is null!");
                            }
                        }
                        catch (Exception ex)
                        {
                            PreNLog("Exception has been thrown while getting value of LeagueRegistryLocationStringName from registry." + Environment.NewLine +
                                                "LeagueRegistrySubKeyName: " + Strings.LeagueRegistrySubKeyName + Environment.NewLine +
                                                "LeagueRegistryLocationStringName: " + Strings.LeagueRegistryLocationStringName + Environment.NewLine +
                                                ex);
                            PreLogWarnings.Add(@"Error while checking registry location string value(Maybe Missing?): " + ex.Message);
                        }

                    }
                }
                catch (Exception ex)
                {
                    PreNLog("Exception has been thrown while opening base key LeagueRegistrySubKeyName from LocalMachine & Registry32." + Environment.NewLine +
                                                "LeagueRegistrySubKeyName: " + Strings.LeagueRegistrySubKeyName + Environment.NewLine +
                                                "LeagueRegistryLocationStringName: " + Strings.LeagueRegistryLocationStringName + Environment.NewLine +
                                                ex);
                    PreLogWarnings.Add(@"Error while checking registry sub key " + Strings.LeagueRegistrySubKeyName + ": " + ex.Message);
                }
                TryDefaultDirectoryPath();
                return PathFound;
            }
        }

        static void TryDefaultDirectoryPath()
        {
            PreNLog("Trying to check if default league directory path exists.");
            try
            {
                if (Directory.Exists(Strings.defaultLeagueDirectoryPath))
                {
                    PreNLog("Successfully found default league directory path: " + Strings.defaultLeagueDirectoryPath);
                    if (UpdateDirPath(Strings.defaultLeagueDirectoryPath))
                    {
                        PreNLog("Default league directory path contains LeagueClient.exe & BsSndRpt.exe.");
                        return;
                    }
                    else
                    {
                        PreNLog("Default directory path doesn't contain LeagueClient.exe & BsSndRpt.exe.");
                    }
                }
                else
                {
                    PreNLog("Default league directory path doesn't exist.");
                }
            }
            catch (Exception ex3)
            {
                PreNLog("Exception has been thrown while trying default league directory path." + Environment.NewLine +
                                    "defaultLeagueDirectoryPath: " + Strings.defaultLeagueDirectoryPath + Environment.NewLine +
                                    ex3);
                PreLogWarnings.Add("Error while setting default LoL path: " + ex3.Message);
            }
            TryRunDirPath();
        }

        static void TryRunDirPath()
        {
            PreNLog("Trying to check if base directory is league's root directory.");
            try
            {
                if (UpdateDirPath(AppDomain.CurrentDomain.BaseDirectory))
                {
                    PreNLog("Successfully found league directory path by checking base directory: " + AppDomain.CurrentDomain.BaseDirectory);
                    return;
                }
                else
                {
                    PreNLog("LeagueClient.exe & BsSndRpt.exe don't exist in base directory, skipping checking this directory: " + AppDomain.CurrentDomain.BaseDirectory);
                }
            }
            catch (Exception ex)
            {
                PreNLog("Exception has been thrown while trying base directory as league's root directory path." + Environment.NewLine + ex);
                PreLogWarnings.Add("Error while setting current directory as LoL path: " + ex.Message);
            }
            TryOpenFileDialogPath();
        }

        static void TryOpenFileDialogPath()
        {
            PreNLog("Trying to let user to select LeagueClient.exe in root directory.");
            try
            {
                if (MessageBox.Show(@"Please select LeagueClient in \Riot Games\League Of Legends folder!", "LeagueFPSBoost: Select Folder", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    PreNLog("User agreed to select LeagueClient.exe in root directory.");
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Select LeagueClient.exe";
                        ofd.Filter = "All Files|*.*|League Client (.exe)|LeagueClient.exe|Any EXE (.exe)|*.exe";
                        ofd.FilterIndex = 2;
                        ofd.Multiselect = false;
                        PreNLog("Showing open file dialog...");
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            PreNLog("User has selected: " + ofd.FileName);
                            if (ofd.FileName.ToLower().Contains("leagueclient.exe"))
                            {
                                PreNLog("User has selected correct file. Checking if LeagueClient.exe & BsSndRpt.exe exist in directory of selected file.");

                                if (UpdateDirPath(Path.GetDirectoryName(ofd.FileName)))
                                {
                                    PreNLog("User has selected correct file in correct folder. league's root directory path is: " + (Path.GetDirectoryName(ofd.FileName)));
                                }
                                else
                                {
                                    PreNLog("BsSndRpt.exe or LeagueClient.exe is missing in selected file's folder.");
                                    PathMissing(true, @"Error while setting custom LoL path: BsSndRpt.exe or LeagueClient.exe missing" + Environment.NewLine + "Maybe wrong file selected?");
                                }
                            }
                            else
                            {
                                PreNLog("User has selected wrong file.");
                                PathMissing(false);
                                MessageBox.Show("Wrong file selected!", "LeagueFPSBoost: Wrong path!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            PreNLog("User has closed open file dialog without selecting file.");
                            PathMissing(false);
                        }
                    }
                }
                else
                {
                    PreNLog("User declined to select LeagueClient.exe in root directory.");
                }
            }
            catch (Exception ex4)
            {
                PreNLog("Exception has been thrown while trying to let user to select LeagueClient.exe in root directory. " + Environment.NewLine + ex4);
                PathMissing(true, "Error while setting custom LoL path: " + ex4.Message);
            }
        }

        static void ClearLog(string logFolderPath)
        {
            var cleared = false;
            int count = 0;
            while (!cleared && ++count < 5)
            {
                try
                {
                    PreNLog("Trying to clear log folder: " + logFolderPath);
                    if (Directory.Exists(logFolderPath))
                    {
                        PreNLog("Directory found. Deleting...");
                        Directory.Delete(logFolderPath, true);
                    }
                    PreNLog("Creating new directory.");
                    Directory.CreateDirectory(logFolderPath);
                    cleared = true;
                }
                catch (Exception ex)
                {
                    PreNLog(Strings.exceptionThrown + " while trying to clear log folder: " + Environment.NewLine + ex);
                    PreNLog("Trying again...");
                }
            }
            PreNLog("Logs cleared.");
        }

        static bool UpdateDirPath(string testPath)
        {
            var lolpath = Path.Combine(testPath);
            PreNLog("Checking if this path is correct before updating directory paths: " + lolpath);
            if (File.Exists(Path.Combine(lolpath, "LeagueClient.exe")) && File.Exists(Path.Combine(lolpath, "BsSndRpt.exe")))
            {
                PreNLog("Specified path contains LeagueClient.exe & BsSndRpt.exe. It is correct.");

                PreNLog("Updating directory paths.");

                LeaguePath = lolpath;
                PreNLog("LeaguePath: " + LeaguePath);

                LeagueLogFileDirPath = Path.Combine(LeaguePath, @"Logs\LeagueFPSBoost Logs\");
                PreNLog("LeagueLogFileDirPath: " + LeagueLogFileDirPath);

                if (ClearLogs) ClearLog(LeagueLogFileDirPath);

                LeagueConfigDirPath = Path.Combine(LeaguePath, @"Config\");
                PreNLog("LeagueConfigDirPath: " + LeagueConfigDirPath);

                LeagueGamePath = Path.Combine(LeaguePath, "Game", "League of Legends.exe");
                PreNLog("LeagueGamePath: " + LeagueGamePath);

                PathFound = true;
                try
                {
                    ConfigureNLogger(Path.Combine(LeagueLogFileDirPath, @"MainLog\"));
                }
                catch (Exception ex)
                {
                    LeagueLogger.Enable();
                    LeagueLogger.Warning("Failed initializing NLogger: " + ex);
                }

                CodeStep = 3; // LeagueFPSBoost: Fatal Error While Configuring Configuration File
                try
                {
                    AppConfig.CreateConfigIfNotExists();

                    Logger.Debug("Saving league's root directory path in settings: " + LeaguePath);
                    Settings.Default.LeaguePath = LeaguePath;
                    Settings.Default.Save();
                    Logger.Debug("Successfully saved.");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, Strings.exceptionThrown + " while configuring application configuration: " + Environment.NewLine);
                    var dialogResult = MessageBox.Show("There was an error while configuring application configuration." + Environment.NewLine +
                                                        "This is known bug when running new version for first time." + Environment.NewLine +
                                                        "If you keep seeing this error check LeagueFPSBoostNLog file." + Environment.NewLine +
                                                        "Restart program automatically now? If no, program will close.", "Known Bug", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if(dialogResult == DialogResult.Yes)
                    {
                        Logger.Debug("Restarting: " + Environment.NewLine + Strings.tabWithLine + "File Name: " + AppConfig.restartInfo.FileName + Environment.NewLine + Strings.tabWithLine + "Arguments: " + AppConfig.restartInfo.Arguments);
                        Process.Start(AppConfig.restartInfo);
                    }
                    Environment.Exit(0);
                }

                return PathFound;
            }
            else
            {
                PreNLog("Specified path doesn't contain LeagueClient.exe & BsSndRpt.exe. Not updating.");
                return false;
            }
        }

        static void StartLogger()
        {
            Logger.Info("League's style logger is not supported anymore.");

            //if (!Directory.Exists(leagueLogFileDirPath))
            //{
            //    Directory.CreateDirectory(leagueLogFileDirPath);
            //    logger.Trace("Created new directory for logging: " + leagueLogFileDirPath);
            //}

            //var hlogFileName = startTime.ToString(Strings.logDateTimeFormat) + "_" + (Process.GetCurrentProcess().Id.ToString()) + "_LeagueFPSBoost.log";
            //var hlogFile = new FileStream(Path.Combine(leagueLogFileDirPath, hlogFileName), FileMode.OpenOrCreate, FileAccess.Write);

            //var textWriterTraceListener = new TextWriterTraceListener(hlogFile);

            //Trace.Listeners.Add(textWriterTraceListener);
            //Trace.AutoFlush = true;

            //LeagueLogger.Enable();
            //LeagueLogger.Info("This logger is disabled at startup.");
            //LeagueLogger.Disable();

            //LeagueLogger.Always("Logging started at " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"));




            //var linkTimeLocal = Assembly.GetExecutingAssembly().GetLinkerTime();
            //var appver = "Application Version:" + CurrentVersion + " - Build Date:" + linkTimeLocal.ToString(" MMMM dd yyyy ") + "- Build Time:" + linkTimeLocal.ToString("HH:mm:ss");
            //crashSb.AppendLine(appver);
            //LeagueLogger.Always(appver);
            //var logFileTxt = "Log file: " + hlogFile.Name.Replace(@"\", @"/");
            //crashSb.AppendLine(logFileTxt);
            //LeagueLogger.Always(logFileTxt);
            //var runningVer = "Running " + Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + " version " + CurrentVersion + " MD5: " + CalculateMD5(Assembly.GetEntryAssembly().Location);
            //crashSb.AppendLine(runningVer);
            //LeagueLogger.Always(runningVer);
            //LeagueLogger.Okay("Initial working directory: \"" + Environment.CurrentDirectory + "\"");
            //LeagueLogger.Okay("Current process: \"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
            //var cmdArgsSB = new StringBuilder();
            //cmdArgsSB.Append("Command line arguments:");
            //if (args.Length != 0) cmdArgsSB.AppendLine();
            //foreach (string arg in args)
            //{
            //    cmdArgsSB.Append(tab).Append(arg).AppendLine();
            //}
            //crashSb.AppendLine(cmdArgsSB.ToString()).AppendLine();
            //LeagueLogger.Okay(cmdArgsSB.ToString());
            //LeagueLogger.Okay("league's root Directory: \"" + leaguePath + "\"");
            //LeagueLogger.Okay(GetLeagueClientInformation(Path.Combine(leaguePath, @"LeagueClient.exe")));
        }

        static string GetLeagueFPSBoostInformation()
        {
            var assembly = Assembly.GetEntryAssembly();
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                var sb = new StringBuilder();


                sb.AppendLine(Strings.tabWithLine + "Path: " + assembly.Location);
                sb.AppendLine(Strings.doubleTabWithLine + "Assembly version: " + CurrentVersionFull);
                sb.AppendLine(Strings.doubleTabWithLine + "Build time: " + assembly.GetLinkerTime());
                sb.AppendLine(Strings.doubleTabWithLine + "File description: " + fvi.FileDescription);
                sb.AppendLine(Strings.doubleTabWithLine + "File version: " + fvi.FileVersion);
                sb.AppendLine(Strings.doubleTabWithLine + "Product name: " + fvi.ProductName);
                sb.AppendLine(Strings.doubleTabWithLine + "Product version: " + fvi.ProductVersion);
                sb.AppendLine(Strings.doubleTabWithLine + "Copyright: " + fvi.LegalCopyright);
                sb.AppendLine(Strings.doubleTabWithLine + "Size: " + GetHumanReadableFileSize(assembly.Location));
                sb.AppendLine(Strings.doubleTabWithLine + "Original file name: " + fvi.OriginalFilename);
                //sb.Append(Strings.doubleTabWithLine + "MD5 checksum: " + CalculateMD5(assembly.Location));
                sb.AppendLine(Strings.doubleTabWithLine + "Checksum: ");
                try
                {
                    var location = assembly.Location;
                    using (var fs = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var calc = new ChecksumCalculator(fs);
                        sb.AppendLine(Strings.tripleTabWithLine + "MD5: " + calc.CalculateMD5());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA1: " + calc.CalculateSHA1());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA256: " + calc.CalculateSHA256());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA384: " + calc.CalculateSHA384());
                        sb.Append(Strings.tripleTabWithLine + "SHA512: " + calc.CalculateSHA512());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while calculating hash for leaguefpsboost file: " + assembly.Location + Environment.NewLine);
                    sb.Append(Strings.tripleTabWithLine + "Error while calculating hash.");
                }
                CrashSb.AppendLine("LeagueFPSBoost info:");
                CrashSb.AppendLine(sb.ToString().TrimEnd());
                CrashSb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while reading LeagueFPSBoost information from file: " + assembly.Location + Environment.NewLine);
                return "ERR";
            }
        }

        static string GetLeagueClientInformation(string clientPath)
        {
            try
            {
                var sb = new StringBuilder();
                var fvi = FileVersionInfo.GetVersionInfo(clientPath);
                

                sb.AppendLine(Strings.tabWithLine + "Path: " + clientPath);
                sb.AppendLine(Strings.doubleTabWithLine + "File description: " + fvi.FileDescription);
                sb.AppendLine(Strings.doubleTabWithLine + "File version: " + fvi.FileVersion);
                sb.AppendLine(Strings.doubleTabWithLine + "Product name: " + fvi.ProductName);
                sb.AppendLine(Strings.doubleTabWithLine + "Product version: " + fvi.ProductVersion);
                sb.AppendLine(Strings.doubleTabWithLine + "Copyright: " + fvi.LegalCopyright);
                sb.AppendLine(Strings.doubleTabWithLine + "Size: " + GetHumanReadableFileSize(clientPath));
                sb.AppendLine(Strings.doubleTabWithLine + "Original file name: " + fvi.OriginalFilename);
                //sb.AppendLine(Strings.doubleTabWithLine + "MD5 checksum: " + CalculateMD5(clientPath));
                sb.AppendLine(Strings.doubleTabWithLine + "Checksum: ");
                try
                {
                    var location = clientPath;
                    
                    using(var fs = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var calc = new ChecksumCalculator(fs);
                        sb.AppendLine(Strings.tripleTabWithLine + "MD5: " + calc.CalculateMD5());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA1: " + calc.CalculateSHA1());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA256: " + calc.CalculateSHA256());
                        sb.AppendLine(Strings.tripleTabWithLine + "SHA384: " + calc.CalculateSHA384());
                        sb.Append(Strings.tripleTabWithLine + "SHA512: " + calc.CalculateSHA512());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while calculating hash for client file: " + clientPath + Environment.NewLine);
                    sb.Append(Strings.tripleTabWithLine + "Error while calculating hash.");
                }
                

                LeagueClientInfo = sb.ToString();
                CrashSb.AppendLine("LeagueClient info:");
                CrashSb.AppendLine(LeagueClientInfo.ToString().TrimEnd());
                CrashSb.AppendLine();
                return LeagueClientInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while reading league client information from file: " + clientPath + Environment.NewLine);
                return "ERR";
            }
        }

        public static string CalculateMD5(string filename)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while calculating MD5 hash for file: " + filename + Environment.NewLine);
                return "ERR";
            }
        }

        static string GetHumanReadableFileSize(string filename)
        {
            try
            {
                var byteSize = ByteSize.FromBytes(new FileInfo(filename).Length);
                return byteSize.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, Strings.exceptionThrown + " while getting human readable file size for file: " + filename + Environment.NewLine);
                return "ERR";
            }
        }

        public static string CurrentVersion
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed
                       ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                       : Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 3);
            }
        }

        public static Version Version
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed
                       ? ApplicationDeployment.CurrentDeployment.CurrentVersion
                       : Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public static string CurrentVersionFull
        {
            get
            {
                return Version.ToString();
            }
        }

        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        public static bool IsAdministrator() => (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);

        public static void PlayNoti(bool success)
        {
            string v = success ? "success" : "fail";
            if (PlayNotiAllow)
            {
                Logger.Debug($"Playing {v} notification.");
                try
                {
                    var waveOut = new WaveOutEvent();
                    if (success)
                    {
                        var mp3SuccessFilePath = Path.Combine(LeaguePath, @"Config\LeagueFPSBoost\notiSuccess.mp3");
                        if (File.Exists(mp3SuccessFilePath))
                        {
                            waveOut.Init(new Mp3FileReader(mp3SuccessFilePath));
                        }
                        else
                        {
                            Logger.Debug("Custom success notification sound not found. Playing default one.");
                            LeagueLogger.Info("Custom success notification sound not found. Playing default one.");

                            waveOut.Init(new Mp3FileReader(new MemoryStream(Resources.notiSuccess)));
                        }
                    }
                    else
                    {
                        var mp3FailFilePath = Path.Combine(LeaguePath, @"Config\LeagueFPSBoost\notiFail.mp3");
                        if (File.Exists(mp3FailFilePath))
                        {
                            waveOut.Init(new Mp3FileReader(mp3FailFilePath));
                        }
                        else
                        {
                            Logger.Debug("Custom fail notification sound not found. Playing default one.");
                            LeagueLogger.Info("Custom fail notification sound not found. Playing default one.");

                            waveOut.Init(new Mp3FileReader(new MemoryStream(Resources.notiFail)));
                        }
                    }
                    waveOut.Play();
                    Logger.Debug("Playing notification succeeded.");
                    LeagueLogger.Okay("Playing notification succeeded.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, Strings.exceptionThrown + " while playing notification: " + Environment.NewLine);
                    LeagueLogger.Error("Playing notification failed: " + ex.Message);
                }
            }
            else
            {
                Logger.Debug($"Not playing {v} notification because its disabled.");
            }
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: leaguefpsboost [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        

        public static void ShowConsole()
        {
            var handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
            Logger.Debug("Console shown.");
        }

        public static void HideConsole(string[] args)
        {
            if(args.Length < 1 || !DebugBuild)
            {
                var handle = NativeMethods.GetConsoleWindow();
                NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
                Logger.Debug("Console hidden.");
            }
        }

        public static bool ConsoleState(bool visible)
        {
            var handle = NativeMethods.GetConsoleWindow();
            while(!HasConsole())
            {
                Logger.Debug("Allocating new console.");
                NativeMethods.AllocConsole();
            }
            ShowConsole();
            if (visible == false) HideConsole(Arguments);
            return visible;
        }

        public static bool HasConsole()
        {
            var handle = NativeMethods.GetConsoleWindow();
            var hasConsole = handle != IntPtr.Zero;
            Logger.Debug("HasConsole: " + hasConsole);
            return hasConsole;
        }
    }

    public class CrashUploadReportEventArgs : EventArgs
    {
        public int Percentage { get; private set; }
        public string Status { get; private set; }

        public CrashUploadReportEventArgs(int perct, string status)
        {
            Percentage = perct;
            Status = status;
        }
    }
}
