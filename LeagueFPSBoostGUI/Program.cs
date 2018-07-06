using ByteSizeLib;
using CrashReporterDotNET;
using LeagueFPSBoost.Configuration;
using LeagueFPSBoost.Diagnostics.Debugger;
using LeagueFPSBoost.GUI;
using LeagueFPSBoost.Logging;
using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Properties;
using LeagueFPSBoost.Text;
using LeagueFPSBoost.Updater.Xml;
using Microsoft.Win32;
using NAudio.Wave;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using StringCipherLib;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Reflection;
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
        public static readonly bool DebugBuild;
#endif

        public static readonly DebuggerWatcher debuggerWatcher = new DebuggerWatcher();

        public static DateTime startTime = DateTime.Now;
        public static readonly Stopwatch stopwatch = Stopwatch.StartNew();

        public static Mutex mutex;

        public static int codeStep;


        public static string leaguePath;
        public static string leagueLogFileDirPath;
        public static string leagueConfigDirPath;
        public static string appConfigDir;

        public static ManagementEventWatcher startWatch;
        public static ManagementEventWatcher stopWatch;

        public static bool noClient;
        public static bool printProcessModules;
        
        public static List<string> preLogWarnings = new List<string>();
        public static List<string> preNLogMessages = new List<string>();
        public static StringBuilder crashSb = new StringBuilder();

        public static StringBuilder piSB = new StringBuilder();
        public static StringBuilder osSB = new StringBuilder();
        public static StringBuilder cpuSB = new StringBuilder();
        public static StringBuilder gpuSB = new StringBuilder();

        public static bool playNotiAllow;
        
        public static bool pathFound;
        
        public static string LeagueClientInfo = "";

        static Logger logger;

        public static bool MainWindowLoaded;
        static readonly bool waitForDebugger = false;

        public static readonly bool mandatoryUpdate = false;

        public static bool UpdatedArg { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            if (waitForDebugger)
            {
                while (!Debugger.IsAttached) { Thread.Sleep(100); }
                Debugger.Break();
            }
            PreNLog("Starting program.");
            PrenlogCmdArgs(args);
            Init();
            CheckArgs(args);
            PreNLog("Enabling Visual Styles.");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            if (!SelfElevation.Elevate()) { return; }
            PreNLog("Current process is in role administrator.");
            
            CreateUpdateFolder(args);

            codeStep = 1; // LeagueFPSBoost: Fatal Error While Checking Mutex
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Check task bar, application is already open.", "LeagueFPSBoost: Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PreNLog("This is only instance of LeagueFPSBoost.");

            codeStep = 2; // LeagueFPSBoost: Fatal Error While Finding League Folder Path
            if (!FindPath())
            {
                MessageBox.Show("Cant find LoL folder path.", "LeagueFPSBoost: Fatal Error While Finding League Folder Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } // logger enabled
                      
            
            if (!CheckForLeagueConfig())
            {
                logger.Fatal("League's game.cfg file is missing. Path: " + Path.Combine(leagueConfigDirPath, @"game.cfg"));
                MessageBox.Show("This program cannot run without configuration file(game.cfg).", "LeagueFPSBoost: Missing LoL configuration file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Startup();
            LeagueLogger.Okay("Starting main window.");
            using (var mainWindow = new MainWindow())
            {
                Application.Run(mainWindow);
            }

            Dispose();
        }

        private static void CreateUpdateFolder(string[] args)
        {
            var dir = Path.Combine(Environment.CurrentDirectory, "Update");
            var zipFileName = "LeagueFPSBoost.zip";
            var zipExtractorName = "ZipExtractor.exe";
            var zipFilePath = Path.Combine(dir, zipFileName);
            var xmlFilePath = Path.Combine(dir, "updater.xml");
            var fileToCompress = Assembly.GetExecutingAssembly().Location;
            var downloadedZip = Path.Combine(Directory.GetCurrentDirectory(), zipFileName);
            var zipExtractorPath = Path.Combine(Directory.GetCurrentDirectory(), zipExtractorName);

            var success = false;

            while (!success)
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
                    foreach (string arg in args)
                    {
                        if (arg.ToLower().Contains(Strings.createUpdateFolderArg.ToLower()))
                        {
                            PreNLog("Trying to create update folder.");
                            if (Directory.Exists(dir)) Directory.Delete(dir, true);
                            Directory.CreateDirectory(dir);

                            using (var newZipFile = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                            {
                                newZipFile.CreateEntryFromFile(fileToCompress, Path.GetFileName(fileToCompress));
                            }

                            PreNLog("Created update zip file: " + zipFilePath);

                            using (var fs = File.OpenRead(zipFilePath))
                            {
                                UpdaterXDocument.Checksum = new Checksum(fs, ChecksumType.SHA512);
                            }

                            UpdaterXDocument.Mandatory = mandatoryUpdate;
                            UpdaterXDocument.Save(xmlFilePath);

                            PreNLog("Created update xml file: " + xmlFilePath);
                            if (!DebugBuild)
                            {
                                MessageBox.Show("Update folder has been created.", "LeagueFPSBoost");
                                Environment.Exit(0);
                            }
                            PreNLog("Update folder has been created!");
                        }
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    PreNLog(Strings.exceptionThrown + " while deleting temporary files or creating update folder: " + Environment.NewLine + ex);
                    PreNLog("Trying again...");
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
            preNLogMessages.Add(msg);
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

            // Step 3. Define rules
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncWrapperFileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncWrapperTraceTarget);

            // Step 4. Activate the configuration
            LogManager.Configuration = config;
            LogManager.ReconfigExistingLoggers();

            logger.Info("Printing info that happened before logger has been initialized.");
            foreach (string s in preNLogMessages)
            {
                logger.Info(s);
            }
            logger.Info("End of printing info that happened before logger has been initialized.");
            logger.Debug("Logger is fully initialized.");
            var task = LogSOFT_HARD_Info(logger);
        }

        static async Task LogSOFT_HARD_Info(Logger log)
        {
            var task = new Task<string>(GetSoftwareAndHardwareInfo);
            log.Trace("Reading system, hardware and software information.");
            task.Start();
            var logTxt = await task;
            log.Debug("Finished reading system and hardware and software information: " + Environment.NewLine + logTxt);
        }

        static string GetSoftwareAndHardwareInfo()
        {
            return ReadOS_CPU_GPU_Info();
        }

        static string ReadOS_CPU_GPU_Info()
        {
            piSB.Append(Process.GetCurrentProcess().GetProcessInfoForLogging(printProcessModules));
            
            var mos = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject mo in mos.Get())
            {
                osSB.AppendLine(Strings.tabWithLine + "Computer name: " + mo["CSName"]);
                osSB.AppendLine(Strings.tabWithLine + "Operating system: ");
                osSB.AppendLine(Strings.doubleTabWithLine + "Caption: " + mo["Caption"]);
                osSB.AppendLine(Strings.doubleTabWithLine + "Architecture: " + mo["OSArchitecture"]);
                osSB.AppendLine(Strings.doubleTabWithLine + "Version: " + mo["Version"]);
                osSB.AppendLine(Strings.doubleTabWithLine + "Build number: " + mo["BuildNumber"]);
                osSB.AppendLine(Strings.doubleTabWithLine + "Build type: " + mo["BuildType"]);
                osSB.AppendLine(Strings.doubleTabWithLine + "Manufacturer: " + mo["Manufacturer"]);
                try
                {
                    var totalMemory = ByteSize.FromKiloBytes(Convert.ToDouble(mo["TotalVisibleMemorySize"]));
                    var freeMemory = ByteSize.FromKiloBytes(Convert.ToDouble(mo["FreePhysicalMemory"]));
                    var usedMemory = totalMemory - freeMemory;
                    osSB.AppendLine(Strings.doubleTabWithLine + "Total Memory: " + totalMemory.ToString("##.## GB"));
                    osSB.AppendLine(Strings.doubleTabWithLine + "Used Memory: " + usedMemory.ToString("##.## GB"));
                    osSB.AppendLine(Strings.doubleTabWithLine + "Free Memory: " + freeMemory.ToString("##.## GB"));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, Strings.exceptionThrown + " while reading system memory." + Environment.NewLine);
                }
                osSB.AppendLine(Strings.doubleTabWithLine + "Product suite: " + mo["OSProductsuite"]);
                osSB.Append(Strings.doubleTabWithLine + "Type: " + mo["OSType"]);
            }

            mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject mo in mos.Get())
            {
                cpuSB.AppendLine(Strings.tabWithLine + mo["DeviceID"] + ":");
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Name: " + mo["Name"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Address width: " + mo["AddressWidth"] + " Bit");
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Data width: " + mo["DataWidth"] + " Bit");
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Cores: " + mo["NumberOfCores"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Enabled cores: " + mo["NumberOfEnabledCore"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Logical Processors: " + mo["NumberOfLogicalProcessors"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Max clock speed: " + mo["MaxClockSpeed"] + " MHz");
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Current clock speed: " + mo["CurrentClockSpeed"] + " MHz");
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Manufacturer: " + mo["Manufacturer"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Socket: " + mo["SocketDesignation"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "Virtualization: " + mo["VirtualizationFirmwareEnabled"]);
                cpuSB.AppendLine(Strings.doubleTabWithLine + "VM Monitor Mode Extensions: " + mo["VMMonitorModeExtensions"]);
            }
            if (cpuSB[cpuSB.Length - 1] == '\n') cpuSB.Remove(cpuSB.Length - 1, 1);

            mos = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject mo in mos.Get())
            {
                gpuSB.AppendLine(Strings.tabWithLine + mo["DeviceID"] + ":");
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Name: " + mo["Name"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Caption: " + mo["Caption"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Processor: " + mo["VideoProcessor"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Adapter compatibility : " + mo["AdapterCompatibility"]);
                try
                {

                    gpuSB.AppendLine(Strings.doubleTabWithLine + "Adapter RAM: " + ByteSize.FromBytes(Convert.ToInt64(mo["AdapterRAM"])).ToString("##.## MB"));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, Strings.exceptionThrown + " while reading video controller memory." + Environment.NewLine);
                }
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Video mode description: " + mo["VideoModeDescription"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Min refresh rate: " + mo["MinRefreshRate"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Max refresh rate: " + mo["MaxRefreshRate"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Current horizontal resolution: " + mo["CurrentHorizontalResolution"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Current vertical resolution: " + mo["CurrentVerticalResolution"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Current refresh rate: " + mo["CurrentRefreshRate"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Current number of colors: " + mo["CurrentNumberOfColors"]);
                gpuSB.AppendLine(Strings.doubleTabWithLine + "Video architecture : " + mo["VideoArchitecture"]);
            }
            if (gpuSB[gpuSB.Length - 1] == '\n') gpuSB.Remove(gpuSB.Length - 1, 1);

            var sb = new StringBuilder();
            sb.AppendLine("System information:");
            sb.AppendLine(osSB.ToString());
            sb.AppendLine("Processor information:");
            sb.AppendLine(cpuSB.ToString());
            sb.AppendLine("Video controller information:");
            sb.AppendLine(gpuSB.ToString());
            sb.AppendLine("Current process information:");
            sb.AppendLine(piSB.ToString());
            sb.AppendLine("LeagueFPSBoost information:");
            sb.AppendLine(GetLeagueFPSBoostInformation());
            sb.AppendLine("League client information:");
            sb.Append(GetLeagueClientInformation(Path.Combine(leaguePath, "LeagueClient.exe")));
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

            PreNLog("Initializing LeaguePriority events.");
            LeaguePriority.GameBoostOk += OnGameBoostOk;
            LeaguePriority.GameBoostFail += OnGameBoostFail;
            LeaguePriority.ClientNormalFail += OnClientNormalFail;
            LeaguePriority.ClientNormalOk += OnClientNormalOk;

            PreNLog("Initializing mutex.");
            mutex = new Mutex(true, @"{63163300-b738-45b6-936f-3b1334617004}");

            logger = LogManager.GetCurrentClassLogger();
        }

        static void Dispose()
        {
            startWatch.Stop();
            stopwatch.Stop();

            startWatch.Dispose();
            stopWatch.Dispose();


            mutex.ReleaseMutex();
        }

        static void Startup()
        {
            

            codeStep = 4; // LeagueFPSBoost: Error While Starting Logging System
            StartLogger();

            startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += ProcessEvents.StartCheckWatch_EventArrived;
            logger.Trace("Subscribed to Win32_ProcessStartTrace event.");

            stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            stopWatch.EventArrived += ProcessEvents.StopCheckWatch_EventArrived;
            logger.Trace("Subscribed to Win32_ProcessStopTrace event.");
        }

        static bool CheckForLeagueConfig()
        {
            return File.Exists(Path.Combine(leagueConfigDirPath, @"game.cfg"));
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
                logger.Fatal(e.Exception, "Application thread exception has been thrown: " + Environment.NewLine);
                ReportCrash(e.Exception);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while creating/sending crash report: " + Environment.NewLine);
                MessageBox.Show("Unknown error: \n" + ex, "LeagueFPSBoost: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LeagueLogger.Error("Current Domain Unhandled Exception Occurred!");
            try
            {
                logger.Fatal((Exception)e.ExceptionObject, "Current domain unhandled exception has been thrown: " + Environment.NewLine);
                ReportCrash((Exception)e.ExceptionObject);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, Strings.exceptionThrown + " while creating/sending crash report: " + Environment.NewLine);
                MessageBox.Show("Unknown error: \n" + ex, "LeagueFPSBoost: Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        public static void ReportCrash(Exception exception)
        {
            logger.Debug("Building crash string for crash report.");
            try
            {
                foreach (string s in preLogWarnings)
                {
                    crashSb.AppendLine(s);
                }
                crashSb.Append("League path var: ").AppendLine(leaguePath);
                crashSb.Append("League configuration directory path var: ").AppendLine(leagueConfigDirPath);
                crashSb.Append("League log directory path var: ").AppendLine(leagueLogFileDirPath);

                LeagueLogger.Error("A crash has been detected: " + Environment.NewLine + Environment.NewLine + exception + Environment.NewLine);
                LeagueLogger.Info("Developer message: " + Environment.NewLine + Environment.NewLine + crashSb);
                LeagueLogger.Okay("Starting CrashReporter.Net");

                ReportCrash2(exception, crashSb.ToString());
                LeagueLogger.Okay("Crash report dialog has been closed.");
                LeagueLogger.Warning("Program will now terminate.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while building crash string for crash report: " + Environment.NewLine);
            }
            
        }

        public static void OnApplicationExit(object sender, EventArgs e)
        {
            logger.Info("Exiting application.");
            LeagueLogger.Always("Exiting program.");
        }

        public static void ReportCrash2(Exception exception, string developerMessage = "")
        {
            logger.Debug("Creating crash report using CrashReporter.Net");
            var reportCrash = new ReportCrash(StringCipher.Decrypt(
                                                            DeveloperData.CrashReport_cipherText,
                                                            DeveloperData.CrashReport_passPharse,
                                                            DeveloperData.CrashReport_keySize))
            {
                DeveloperMessage = developerMessage,
                CaptureScreen = true,
                EmailRequired = false,
                IncludeScreenshot = true
            };
            var sb = new StringBuilder();
            sb.AppendLine(Strings.tabWithLine + "Crash report:");
            sb.AppendLine(Strings.doubleTabWithLine + "Capture screen: " + reportCrash.CaptureScreen);
            sb.AppendLine(Strings.doubleTabWithLine + "Email required: " + reportCrash.EmailRequired);
            sb.Append(Strings.doubleTabWithLine + "Include screenshot: " + reportCrash.IncludeScreenshot);
            logger.Debug("Crash report has been created: " + Environment.NewLine + sb);
            logger.Debug("Displaying crash report window.");
            try
            {
                reportCrash.Send(exception);
                logger.Debug("Crash report window has been closed. Developer message: " + Environment.NewLine + reportCrash.DeveloperMessage);
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while displaying crash report window: " + Environment.NewLine);
            }
        }
        
        static void PathMissing(bool showMsgBox)
        {
            pathFound = false;
            if (showMsgBox) MessageBox.Show("Cannot find path for League Of Legends in registry!", "LeagueFPSBoost: Can't Set Game Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void PathMissing(bool showMsgBox, string msg)
        {
            pathFound = false;
            if (showMsgBox) MessageBox.Show(msg, "LeagueFPSBoost: Can't Set Game Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static bool FindPath()
        {
            PreNLog("Trying to find league's root directory path in settings file.");
            try
            {
                var settingsLeaguePath = Settings.Default.LeaguePath;
                PreNLog("Got settingsLeaguePath from settings: " + settingsLeaguePath);
                if (settingsLeaguePath == "") throw new ArgumentException("League's root directory path from settings is empty.");
                PreNLog("Checking if path from settings exists.");
                if (Directory.Exists(settingsLeaguePath))
                {
                    PreNLog("Checking if path from settings is correct.");
                    if(UpdateDirPath(settingsLeaguePath))
                    {
                        PreNLog("Successfully found path in settings: " + settingsLeaguePath);
                        return pathFound;
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
                                    return pathFound;
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
                            preLogWarnings.Add(@"Error while checking registry location string value(Maybe Missing?): " + ex.Message);
                        }

                    }
                }
                catch (Exception ex)
                {
                    PreNLog("Exception has been thrown while opening base key LeagueRegistrySubKeyName from LocalMachine & Registry32." + Environment.NewLine +
                                                "LeagueRegistrySubKeyName: " + Strings.LeagueRegistrySubKeyName + Environment.NewLine +
                                                "LeagueRegistryLocationStringName: " + Strings.LeagueRegistryLocationStringName + Environment.NewLine +
                                                ex);
                    preLogWarnings.Add(@"Error while checking registry sub key " + Strings.LeagueRegistrySubKeyName + ": " + ex.Message);
                }
                TryDefaultDirectoryPath();
                return pathFound;
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
                    if(UpdateDirPath(Strings.defaultLeagueDirectoryPath))
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
                preLogWarnings.Add("Error while setting default LoL path: " + ex3.Message);                
            }
            TryRunDirPath();
        }

        static void TryRunDirPath()
        {
            PreNLog("Trying to check if base directory is league's root directory.");
            try
            {
                if(UpdateDirPath(AppDomain.CurrentDomain.BaseDirectory))
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
                preLogWarnings.Add("Error while setting current directory as LoL path: " + ex.Message);
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

                                if(UpdateDirPath(Path.GetDirectoryName(ofd.FileName)))
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

        static bool UpdateDirPath(string testPath)
        {
            var lolpath = Path.Combine(testPath);
            PreNLog("Checking if this path is correct before updating directory paths: " + lolpath);
            if (File.Exists(Path.Combine(lolpath, "LeagueClient.exe")) && File.Exists(Path.Combine(lolpath, "BsSndRpt.exe")))
            {
                PreNLog("Specified path contains LeagueClient.exe & BsSndRpt.exe. It is correct.");

                PreNLog("Updating directory paths.");

                leaguePath = lolpath;
                PreNLog("leaguePath: " + leaguePath);

                leagueLogFileDirPath = Path.Combine(leaguePath, @"Logs\LeagueFPSBoost Logs\");
                PreNLog("leagueLogFileDirPath: " + leagueLogFileDirPath);

                leagueConfigDirPath = Path.Combine(leaguePath, @"Config\");
                PreNLog("leagueConfigDirPath: " + leagueConfigDirPath);

                pathFound = true;
                try
                {
                    ConfigureNLogger(Path.Combine(leagueLogFileDirPath, @"MainLog\"));
                }
                catch(Exception ex)
                {
                    LeagueLogger.Enable();
                    LeagueLogger.Warning("Failed initializing NLogger: " + ex);
                }

                codeStep = 3; // LeagueFPSBoost: Fatal Error While Configuring Configuration File
                try
                {
                    AppConfig.CreateConfigIfNotExists();

                    logger.Debug("Saving league's root directory path in settings: " + leaguePath);
                    Settings.Default.LeaguePath = leaguePath;
                    Settings.Default.Save();
                    logger.Debug("Successfully saved.");
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, Strings.exceptionThrown + " while configuring application configuration: " + Environment.NewLine);
                    MessageBox.Show("There was an error while configuring application configuration." + Environment.NewLine + 
                                    "This is known bug when running new version for first time." + Environment.NewLine +
                                    "If you keep seeing this error check LeagueFPSBoostNLog file." + Environment.NewLine + 
                                    "You can try to restart program for now. Program will close.", "Known Bug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                }

                return pathFound;
            }
            else
            {
                PreNLog("Specified path doesn't contain LeagueClient.exe & BsSndRpt.exe. Not updating.");
                return false;
            }
        }

        static void StartLogger()
        {
            logger.Info("League's style logger is not supported anymore.");

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
                sb.Append(Strings.doubleTabWithLine + "MD5 checksum: " + CalculateMD5(assembly.Location));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while reading LeagueFPSBoost information from file: " + assembly.Location + Environment.NewLine);
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
                sb.Append(Strings.doubleTabWithLine + "MD5 checksum: " + CalculateMD5(clientPath));

                LeagueClientInfo = sb.ToString();
                crashSb.AppendLine(LeagueClientInfo).AppendLine();
                return LeagueClientInfo;
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while reading league client information from file: " + clientPath + Environment.NewLine);
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
                logger.Error(ex, Strings.exceptionThrown + " while calculating MD5 hash for file: " + filename + Environment.NewLine);
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
                logger.Error(ex, Strings.exceptionThrown + " while getting human readable file size for file: " + filename + Environment.NewLine);
                return "ERR";
            }
        }

        static void CheckArgs(string[] args)
        {
            foreach (string arg in args)
            {
                var argLower = arg.ToLower();
                noClient = argLower == Strings.noClientArg.ToLower();
                printProcessModules = argLower == Strings.printProcessModulesArg.ToLower();
                if (argLower == Strings.updateCheckArg.ToLower())
                {
                    PreNLog("Update check argument has been specified. Trying to open boards page.");
                    try
                    {
                        Process.Start("https://goo.gl/bpxbGV");
                        PreNLog("Boards page opened.");
                        LeagueLogger.Okay("Update link opened.");
                    }
                    catch (Exception ex)
                    {
                        PreNLog("Exception has been thrown while opening boards page: " + Environment.NewLine + ex);
                        LeagueLogger.Error("Error while opening update link: " + ex.Message);
                    }
                }
                if(argLower == Strings.adminRestartReasonArg.ToLower())
                {
                    PreNLog("Program has restarted itself with admin rights.");
                }
                if(argLower == Strings.configRestartReasonArg.ToLower())
                {
                    PreNLog("Program has restarted itself because there was an error with configuration.");
                }
                if(argLower == Strings.updatedArg.ToLower())
                {
                    PreNLog("Application has been updated!");
                    UpdatedArg = true;
                }
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
                return ApplicationDeployment.IsNetworkDeployed
                       ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                       : Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
            if (playNotiAllow)
            {
                logger.Debug($"Playing {v} notification.");
                try
                {
                    var waveOut = new WaveOutEvent();
                    if (success)
                    {
                        var mp3SuccessFilePath = Path.Combine(leaguePath, @"Config\LeagueFPSBoost\notiSuccess.mp3");
                        if (File.Exists(mp3SuccessFilePath))
                        {
                            waveOut.Init(new Mp3FileReader(mp3SuccessFilePath));
                        }
                        else
                        {
                            logger.Debug("Custom success notification sound not found. Playing default one.");
                            LeagueLogger.Info("Custom success notification sound not found. Playing default one.");
                            
                            waveOut.Init(new Mp3FileReader(new MemoryStream(Resources.notiSuccess)));
                        }
                    }
                    else
                    {
                        var mp3FailFilePath = Path.Combine(leaguePath, @"Config\LeagueFPSBoost\notiFail.mp3");
                        if (File.Exists(mp3FailFilePath))
                        {
                            waveOut.Init(new Mp3FileReader(mp3FailFilePath));
                        }
                        else
                        {
                            logger.Debug("Custom fail notification sound not found. Playing default one.");
                            LeagueLogger.Info("Custom fail notification sound not found. Playing default one.");
                            
                            waveOut.Init(new Mp3FileReader(new MemoryStream(Resources.notiFail)));
                        }
                    }
                    waveOut.Play();
                    logger.Debug("Playing notification succeeded.");
                    LeagueLogger.Okay("Playing notification succeeded.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, Strings.exceptionThrown + " while playing notification: " + Environment.NewLine);
                    LeagueLogger.Error("Playing notification failed: " + ex.Message);
                }
            }
            else
            {
                logger.Debug($"Not playing {v} notification because its disabled.");
            }
        }
    }
}
