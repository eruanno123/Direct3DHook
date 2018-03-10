
namespace TestScreenshot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Linq;
    using TestScreenshot.Logger;
    using TestScreenshot.Helpers;
    using Capture.Hook;
    using System.Diagnostics;
    using Capture;
    using Capture.Interface;
    using System.Drawing;

    public sealed class HellbladeGameSaveTracker : IDisposable
    {
        private HellbladeTrackingConfig _config;
        private FileSystemWatcher _fileWatcher;

        private List<HSaveItem> _saveList = new List<HSaveItem>();

        private bool _shouldExit;

        public HellbladeGameSaveTracker (HellbladeTrackingConfig config)
        {
            _config = config;
            Initialize();
        }

        private void Initialize ()
        {
            _fileWatcher = new FileSystemWatcher()
            {
                Path = _config.SaveGamePath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                Filter = _config.SaveGameFilter
            };

            _fileWatcher.Changed += _fileWatcher_Changed;
            _fileWatcher.EnableRaisingEvents = true;

            Directory.CreateDirectory(_config.SaveBackupPath);
        }

        private void _fileWatcher_Changed (object sender, FileSystemEventArgs e)
        {
            SimpleLogger.Default.Info("File Event = {0}, Name = {1}", e.ChangeType.ToString(), e.Name);

            if (File.Exists(e.FullPath))
            {
                var hash = MD5Helper.GetMD5String(e.FullPath);

                if (!_saveList.Exists(hs => hash == hs.Checksum))
                {
                    int nCount = _saveList.Count;
                    string locName = string.Format(_config.DefaultNameFormat, nCount);
                    string backupPath = PathHelper.AddBackslash(_config.SaveBackupPath) + locName + ".sav";
                    while (File.Exists(backupPath))
                    {
                        nCount++;
                        locName = string.Format(_config.DefaultNameFormat, nCount);
                        backupPath = PathHelper.AddBackslash(_config.SaveBackupPath) + locName + ".sav";
                    }

                    string imgPath = PathHelper.AddBackslash(_config.SaveBackupPath) + locName + ".jpg";

                    HSaveItem hSaveItem = new HSaveItem()
                    {
                        SaveFilePath = backupPath,
                        ScreenCaptureFilePath = imgPath,
                        CaptureTime = DateTime.Now,
                        Checksum = hash,
                        LocationName = locName
                    };
                    _saveList.Add(hSaveItem);

                
                    File.Copy(e.FullPath, backupPath);
                    GrabScreenCapture()?.Save(imgPath);

                    SimpleLogger.Default.Info("New save location: {0}", hSaveItem);
                }
            }
        }

        public void Dispose ()
        {
            _fileWatcher.Dispose();
        }

        public void Run ()
        {
            if (_workerThread == null)
            {
                _workerThread = new Thread(new ThreadStart(WorkerThread))
                {
                    IsBackground = true
                };
                _workerThread.Start();
            }
        }

        public void Cancel ()
        {
            if (_workerThread != null)
            {
                _shouldExit = true;
                _workerThread.Join();
                _workerThread = null;
            }
        }

        private bool _isAttached;

        private Thread _workerThread;
        private void WorkerThread ()
        {
            while (!_shouldExit)
            {
                if (!_isAttached)
                {
                    try
                    {
                        AttachProcess();
                    }
                    catch (Exception e)
                    {
                        SimpleLogger.Default.Warning("Cannot attach to game process: {0}", e.Message);
                    }
                }

                Thread.Sleep(1000);
            }

            SimpleLogger.Default.Info("Detaching from game process ...");
            if (_isAttached)
            {
                DetachProcess();
            }
        }

        /************ SCREEN CAPTURE LOGIC *************/

        private void DetachProcess ()
        {
            if (_captureProcess != null)
            {
                HookManager.RemoveHookedProcess(_captureProcess.Process.Id);
                _captureProcess.CaptureInterface.Disconnect();
                _captureProcess = null;
            }

            _isAttached = false;
        }

        int processId = 0;
        Process _process;
        CaptureProcess _captureProcess;
        private void AttachProcess ()
        {
            string exeName = Path.GetFileNameWithoutExtension(_config.HellbladeExecutablePath);

            Process[] processes = Process.GetProcessesByName(exeName);
            foreach (Process process in processes)
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    continue;
                }

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = Direct3DVersion.AutoDetect,
                    ShowOverlay = false
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);
                _captureProcess = new CaptureProcess(process, cc, captureInterface);

                break;
            }
            Thread.Sleep(10);

            if (_captureProcess == null)
            {
                throw new InvalidOperationException($"No executable found matching: '{exeName}''");
            }

            _isAttached = true;
        }

        /// <summary>
        /// Display messages from the target process
        /// </summary>
        /// <param name="message"></param>
        private void CaptureInterface_RemoteMessage (MessageReceivedEventArgs message)
        {
            SimpleLogger.Default.Info("{0}", message);
        }

        /// <summary>
        /// Display debug messages from the target process
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="message"></param>
        void ScreenshotManager_OnScreenshotDebugMessage (int clientPID, string message)
        {
            SimpleLogger.Default.Trace("{0}:{1}", clientPID, message);
        }

        /// <summary>
        /// Create the screen shot request
        /// </summary>
        Bitmap GrabScreenCapture()
        {
            if (_isAttached)
            {
                var screenshot = _captureProcess.CaptureInterface.GetScreenshot();

                if ((screenshot != null) && (screenshot.Data != null))
                {
                    return screenshot.ToBitmap();
                }
            }

            return null;
        }
    }
}
