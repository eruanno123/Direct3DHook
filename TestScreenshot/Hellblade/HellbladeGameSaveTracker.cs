
namespace HellbladeSaver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Linq;
    using HellbladeSaver.Logger;
    using HellbladeSaver.Helpers;
    using Capture.Hook;
    using System.Diagnostics;
    using Capture;
    using Capture.Interface;
    using System.Drawing;

    public sealed class HellbladeGameSaveTracker : IDisposable
    {
        public HellbladeGameSaveTracker (HellbladeTrackingConfig config)
        {
            _config = config;
            Initialize();
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

        /***************** PRIVATE METHODS ********************/

        private void Initialize ()
        {
            _fileWatcher = new FileSystemWatcher()
            {
                Path = _config.SaveGamePath,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = _config.SaveGameFilter
            };

            _fileWatcher.Changed += _fileWatcher_Changed;
            _fileWatcher.EnableRaisingEvents = true;

            Directory.CreateDirectory(_config.SaveBackupPath);
        }

        private string GenerateLocationName(string contentHash)
        {
            string locName = string.Empty;

            do
            {
                locName = string.Format(_config.DefaultNameFormat, DateTime.Now, _saveCounter, contentHash.Substring(0, 6));
                _saveCounter++;
            } while (File.Exists(locName + ".sav"));

            return locName;
        }

        private void _fileWatcher_Changed (object sender, FileSystemEventArgs e)
        {
            SimpleLogger.Default.Trace($"File Event = {e.ChangeType}, Name = {e.Name}, Path = {e.FullPath}");
            Thread.Sleep(100);

            try
            {
                PerformSavegameBackup(e.FullPath);
            }
            catch (Exception ex)
            {
                SimpleLogger.Default.Error("Problem with saving backup: " + ex.Message);
            }
        }

        private void PerformSavegameBackup (string trackedSavFile)
        {
            if (File.Exists(trackedSavFile))
            {
                var hash = MD5Helper.GetMD5String(trackedSavFile);

                if (!_saveList.Exists(hs => hash == hs.Checksum))
                {
                    string locName = GenerateLocationName(hash);
                    string backupBasePath = PathHelper.AddBackslash(_config.SaveBackupPath) + locName;
                    string backupPath = backupBasePath + ".sav";
                    string screenCapturePath = backupBasePath + ".jpg";

                    HellbladeSaveItem hSaveItem = new HellbladeSaveItem()
                    {
                        SaveFilePath = backupPath,
                        ScreenCaptureFilePath = screenCapturePath,
                        CaptureTime = DateTime.Now,
                        Checksum = hash,
                        LocationName = locName
                    };
                    _saveList.Add(hSaveItem);

                    File.Copy(trackedSavFile, backupPath);
                    GrabScreenCapture()?.Save(screenCapturePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                    SimpleLogger.Default.Info("New save location: {0}", hSaveItem);
                }
            }
        }

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
                    catch (InvalidOperationException e)
                    {
                        SimpleLogger.Default.Trace("Cannot attach to game process (yet): {0}", e.Message);
                    }
                    catch (Exception e)
                    {
                        SimpleLogger.Default.Fatal("Failed to attach to game process: {0}", e.Message);
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
        private void ScreenshotManager_OnScreenshotDebugMessage (int clientPID, string message)
        {
            SimpleLogger.Default.Trace("{0}:{1}", clientPID, message);
        }

        /// <summary>
        /// Create the screen shot request
        /// </summary>
        private Bitmap GrabScreenCapture()
        {
            if (_isAttached)
            {
                var screenshot = _captureProcess.CaptureInterface.GetScreenshot(
                    Rectangle.Empty, TimeSpan.FromSeconds(2), _config.TargetImageSize, ImageFormat.Bitmap);

                if ((screenshot != null) && (screenshot.Data != null))
                {
                    return screenshot.ToBitmap();
                }
            }

            return null;
        }

        #region Private stuff
        private HellbladeTrackingConfig _config;
        private FileSystemWatcher _fileWatcher;
        private List<HellbladeSaveItem> _saveList = new List<HellbladeSaveItem>();
        private int _saveCounter;
        private bool _shouldExit;
        private bool _isAttached;
        private Thread _workerThread;
        private Process _process;
        private CaptureProcess _captureProcess;
        #endregion
    }
}
