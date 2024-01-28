﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.Resource.App
{
    public enum Application
    {
        NotSet,
        ModInstaller,
        EepkOrganiser,
        Ace,
        XenoKit
    }

    public class SettingsManager
    {
        //Singleton
        private static Lazy<SettingsManager> instance = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance => instance.Value;
        public static Settings settings => instance.Value.Settings;

        //Settings
        public Settings Settings { get; set; }
        public Application CurrentApp { get; set; } = Application.NotSet;

        #region SharedSettings
        //These settings are shared between some applications and so must always be available

        public bool TextureImportMatchNames { get { return ReflectionHelper.GetBoolProp(Settings, "TextureReuse_NameMatch", true); } }
        public bool LoadTextures { get { return ReflectionHelper.GetBoolProp(Settings, "LoadTextures", true); } }
        public bool AutoContainerRename { get { return ReflectionHelper.GetBoolProp(Settings, "AutoContainerRename", false); } }
        public bool AssetReuseMatchName { get { return ReflectionHelper.GetBoolProp(Settings, "AssetReuse_NameMatch", true); } }

        #endregion

        //Version
        public string CurrentVersionString
        {
            get
            {
                string[] split = Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.');
                if (split[2] == "0" && split[3] == "0")
                {
                    return String.Format("{0}.{1}", split[0], split[1]);
                }
                else if (split[3] == "0")
                {
                    return String.Format("{0}.{1}.{2}", split[0], split[1], split[2]);
                }
                else
                {
                    return String.Format("{0}.{1}.{2}.{3}", split[0], split[1], split[2], split[3]);
                }
            }
        }
        public Version CurrentVersion { get { return Assembly.GetEntryAssembly().GetName().Version; } }

        //FileWatcher
        private FileSystemWatcher watcher;
        private DateTime lastSaveTime = DateTime.Now;
        private bool reloadTriggered = false;

        //Events
        public static event EventHandler SettingsReloaded;
        public static event EventHandler SettingsSaved;

        private SettingsManager()
        {
            LoadSettings();

            //Init file watcher
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(GetSetingsPath());
            watcher.Filter = Path.GetFileName(GetSetingsPath());
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += SettingsChangedExternally;
            watcher.EnableRaisingEvents = true;
        }

        private async void SettingsChangedExternally(object sender, FileSystemEventArgs e)
        {
            if (reloadTriggered) return;

            //Reload settings becuase it was changed externally
            if (lastSaveTime < (DateTime.Now - new TimeSpan(0, 0, 5)) && !reloadTriggered)
            {
                reloadTriggered = true;

                //Delay for 30 seconds if settings file is locked
                await Task.Run(() =>
                {
                    int locked = 0;

                    while (Utils.IsFileWriteLocked(GetSetingsPath()))
                    {
                        if (locked >= 30000)
                        {
                            break;
                        }

                        Task.Delay(100);
                        locked += 100;
                    }
                });

                //If settings is still locked, do not attempt to load it
                if (Utils.IsFileWriteLocked(GetSetingsPath()))
                {
                    reloadTriggered = false;
                    return;
                }

                LoadSettings(true);

                reloadTriggered = false;
            }
        }

        private void LoadSettings(bool isReload = false)
        {
            //if (CurrentApp == Application.NotSet) throw new InvalidOperationException("SettingsManager.LoadSettings: CurrentApp has not been set.");

#if DEBUG
            //In release build, rely on the try/catch instead
            if (!File.Exists(GetSetingsPath()))
            {
                Settings = new Settings();

                Settings.InitSettings();

                SaveSettings();
                return;
            }
#endif


            try
            {

                //Try to load the settings
                var oldSettings = Settings;
                var settings = Settings.Load(GetSetingsPath());
                settings.InitSettings();
                settings.ValidateSettings();

                Settings = settings;
                SettingsReloaded?.Invoke(oldSettings, EventArgs.Empty);

            }
            catch
            {
                if (!isReload)
                {
                    //If it fails, create a new instance and save it to disk.
                    Settings = new Settings();

                    Settings.InitSettings();

                    SaveSettings();
                }
            }

        }

        public void SaveSettings(bool errorIfFail = true)
        {
            //if (CurrentApp == Application.NotSet) throw new InvalidOperationException("SettingsManager.LoadSettings: CurrentApp has not been set.");
            if (Settings == null) throw new Exception("SettingsManager.SaveSettings: No settings are loaded.");

            SettingsSaved?.Invoke(this, EventArgs.Empty);

#if !DEBUG
            try
            {
#endif
                Settings.ValidateSettings();
                Directory.CreateDirectory(Path.GetDirectoryName(GetSetingsPath()));

                Settings.Save(GetSetingsPath());
                lastSaveTime = DateTime.Now;
#if !DEBUG
            }
            catch (UnauthorizedAccessException)
            {
                if (errorIfFail)
                    MessageBox.Show("Failed to save settings.\n\nThe application does not have write access in the current directory. Try moving it somewhere else or running it as administrator.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch
            {
                if (errorIfFail)
                    MessageBox.Show("Failed to save settings.", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif

        }


        /// <summary>
        /// Get current theme as a string formatted for MahApps.Metro 2.0+.
        /// </summary>
        public string GetTheme()
        {
            string baseTheme = settings.UseDarkTheme ? "Dark" : "Light";
            AppAccent accent = settings.UseDarkTheme ? settings.CurrentDarkAccent : settings.CurrentLightAccent;
            return $"{baseTheme}.{accent.ToString()}";
        }

        #region PathHelpers
        public static string GetAbsolutePathRelativeToExe(string relativePath)
        {
            return String.Format("{0}/{1}", Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), relativePath);
        }

        public string GetSetingsPath()
        {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/LBTools/settings.txt";

            /*
            switch (CurrentApp)
            {
                case Application.EepkOrganiser:
                case Application.Ace:
                case Application.XenoKit:
                case Application.ModInstaller:
                    return $"{GetAppFolder()}/settings.xml";
                default:
                    return "settings.xml";
            }
            */
        }

        public string GetErrorLogPath()
        {
            switch (CurrentApp)
            {
                case Application.EepkOrganiser:
                case Application.Ace:
                case Application.XenoKit:
                    return $"{GetAppFolder()}/error_log.txt";
                default:
                    return GetAbsolutePathRelativeToExe("error_log.txt");
            }
        }

        public string GetAppFolder()
        {
            return GetAppFolder(CurrentApp);
        }

        public string GetAppFolder(Application app)
        {
            switch (app)
            {
                case Application.EepkOrganiser:
                    return GetAbsolutePathRelativeToExe("eepk_tool");
                case Application.Ace:
                    return GetAbsolutePathRelativeToExe("ace");
                case Application.XenoKit:
                    return GetAbsolutePathRelativeToExe("XenoKit");
                case Application.ModInstaller:
                    return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/LB Mod Installer 3";
                default:
                    return "";
            }
        }

        public string GetAbsPathInAppFolder(string relativePath)
        {
            return $"{GetAppFolder()}/{relativePath}";
        }

        public static bool IsGameDirValid(string path)
        {
            return (File.Exists(String.Format("{0}/bin/DBXV2.exe", path)));
        }
        #endregion

    }

    public class Settings : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //Binary
        private SettingsFormat settingsBinary = null;

        //Only basic value types are allowed + AppAccent. This is because SettingsFormat only supports these types, and adding more is not allowed because it will break backwards-compatibility.
        //All props with a Get/Setter will be serialized to a txt file, everything else is ignored

        //Helper props
        public bool ValidGameDir
        {
            get
            {
                return SettingsManager.IsGameDirValid(GameDirectory);
            }
        }


        #region Values
        //Common
        private string _gameDir = string.Empty;
        private string _savFile = string.Empty;
        private int _undoLimit = UndoManager.DefaultMaxCapacity;
        private AppTheme _currentTheme = AppTheme.Light;
        private AppAccent _currentLightAccent = AppAccent.Blue;
        private AppAccent _currentDarkAccent = AppAccent.Emerald;

        //EepkOrganiser
        private bool _assetReuseNameMatch = false;
        private EepkTextureReuse _textureReuse = EepkTextureReuse.Identical;
        private EepkFileCleanUp _fileCleanUp = EepkFileCleanUp.Prompt;
        private bool _autoContainerRename = true;
        private int _fileCacheLimit = 10;
        #endregion

        #region Common
        public int SettingsVersion { get; set; } = 3;

        public bool UpdateNotifications { get; set; } = true;
        public string GameDirectory
        {
            get => _gameDir;
            set
            {
                if (value != _gameDir)
                {
                    _gameDir = value;
                    NotifyPropertyChanged(nameof(GameDirectory));
                }
            }
        }
        public string SaveFile
        {
            get => _savFile;
            set
            {
                if (value != _savFile)
                {
                    _savFile = value;
                    NotifyPropertyChanged(nameof(SaveFile));
                }
            }
        }

        public bool UseLightTheme
        {
            get => _currentTheme == AppTheme.Light;
            set
            {
                if (value)
                    _currentTheme = AppTheme.Light;

                NotifyPropertyChanged(nameof(UseLightTheme));
            }
        }
        public bool UseDarkTheme
        {
            get => _currentTheme == AppTheme.Dark;
            set
            {
                if (value)
                    _currentTheme = AppTheme.Dark;

                NotifyPropertyChanged(nameof(UseDarkTheme));
            }
        }
        public int UndoLimit
        {
            get => _undoLimit;
            set
            {
                if (value != _undoLimit)
                {
                    _undoLimit = value;
                    NotifyPropertyChanged(nameof(UndoLimit));
                }
            }
        }

        public AppAccent CurrentLightAccent { get => _currentLightAccent; set { _currentLightAccent = value; NotifyPropertyChanged(nameof(CurrentLightAccent)); } }
        public AppAccent CurrentDarkAccent { get => _currentDarkAccent; set { _currentDarkAccent = value; NotifyPropertyChanged(nameof(CurrentDarkAccent)); } }
        public bool DefaultThemeSet { get; set; }
        #endregion

        #region EepkOrganiser
        public bool LoadTextures { get; set; } = true;
        public bool AssetReuse_NameMatch
        {
            get => _assetReuseNameMatch;
            set
            {
                if (value != _assetReuseNameMatch)
                {
                    _assetReuseNameMatch = value;
                    NotifyPropertyChanged(nameof(AssetReuse_NameMatch));
                }
            }
        }
        public bool TextureReuse_Identical
        {
            get => _textureReuse == EepkTextureReuse.Identical;
            set
            {
                if (value && !TextureReuse_Identical)
                {
                    _textureReuse = EepkTextureReuse.Identical;
                    NotifyPropertyChanged(nameof(TextureReuse_NameMatch));
                    NotifyPropertyChanged(nameof(TextureReuse_Identical));
                }
            }
        }
        public bool TextureReuse_NameMatch
        {
            get => _textureReuse == EepkTextureReuse.NameMatch;
            set
            {
                if (value && !TextureReuse_NameMatch)
                {
                    _textureReuse = EepkTextureReuse.NameMatch;
                    NotifyPropertyChanged(nameof(TextureReuse_NameMatch));
                    NotifyPropertyChanged(nameof(TextureReuse_Identical));
                }
            }
        }
        public bool FileCleanUp_Delete
        {
            get => _fileCleanUp == EepkFileCleanUp.Delete;
            set
            {
                if (value && !FileCleanUp_Delete)
                {
                    _fileCleanUp = EepkFileCleanUp.Delete;
                    NotifyPropertyChanged(nameof(FileCleanUp_Ignore));
                    NotifyPropertyChanged(nameof(FileCleanUp_Prompt));
                    NotifyPropertyChanged(nameof(FileCleanUp_Delete));
                }
            }
        }
        public bool FileCleanUp_Prompt
        {
            get => _fileCleanUp == EepkFileCleanUp.Prompt;
            set
            {
                if (value && !FileCleanUp_Prompt)
                {
                    _fileCleanUp = EepkFileCleanUp.Prompt;
                    NotifyPropertyChanged(nameof(FileCleanUp_Ignore));
                    NotifyPropertyChanged(nameof(FileCleanUp_Prompt));
                    NotifyPropertyChanged(nameof(FileCleanUp_Delete));
                }
            }
        }
        public bool FileCleanUp_Ignore
        {
            get => _fileCleanUp == EepkFileCleanUp.Ignore;
            set
            {
                if (value && !FileCleanUp_Ignore)
                {
                    _fileCleanUp = EepkFileCleanUp.Ignore;
                    NotifyPropertyChanged(nameof(FileCleanUp_Ignore));
                    NotifyPropertyChanged(nameof(FileCleanUp_Prompt));
                    NotifyPropertyChanged(nameof(FileCleanUp_Delete));
                }
            }
        }
        public bool AutoContainerRename
        {
            get => _autoContainerRename;
            set
            {
                if (value != _autoContainerRename)
                {
                    _autoContainerRename = value;
                    NotifyPropertyChanged(nameof(AutoContainerRename));
                }
            }
        }
        public int FileCacheLimit
        {
            get => _fileCacheLimit;
            set
            {
                if (value != _fileCacheLimit)
                {
                    _fileCacheLimit = value;
                    NotifyPropertyChanged(nameof(FileCacheLimit));
                }
            }
        }

        //EffectPart Tabs
        public bool EepkOrganiser_EffectPart_General_Expanded { get; set; } = true;
        public bool EepkOrganiser_EffectPart_Position_Expanded { get; set; } = true;
        public bool EepkOrganiser_EffectPart_Animation_Expanded { get; set; } = false;
        public bool EepkOrganiser_EffectPart_Flags_Expanded { get; set; } = true;
        public bool EepkOrganiser_EffectPart_UnkFlags_Expanded { get; set; }
        public bool EepkOrganiser_EffectPart_UnkValues_Expanded { get; set; }

        #endregion

        #region XenoKit
        public int XenoKit_WindowSizeX { get; set; } = -1;
        public int XenoKit_WindowSizeY { get; set; } = -1;
        public int XenoKit_DelayedUpdateFrameInterval { get; set; } = 30;
        public bool XenoKit_HideEmptyBacEntries { get; set; } = true;
        public bool XenoKit_EnableCameraAnimations { get; set; } = true;
        public bool XenoKit_EnableVisualSkeleton { get; set; } = true;
        public bool XenoKit_AutoPlay { get; set; } = true;
        public bool XenoKit_Loop { get; set; } = false;
        public bool XenoKit_AudioSimulation { get; set; } = true;
        public bool XenoKit_HitboxSimulation { get; set; } = true;
        public bool XenoKit_ProjectileSimulation { get; set; } = true;
        public bool XenoKit_VfxSimulation { get; set; } = true;
        public bool XenoKit_PreserveCameraState { get; set; } = true;
        public bool XenoKit_RenderBoneNames { get; set; } = true;
        public bool XenoKit_RenderBoneNamesMouseOverOnly { get; set; } = true;
        public bool XenoKit_HideLessImportantBones { get; set; } = true;
        public bool XenoKit_AutoResolvePasteReferences { get; set; } = false;
        internal int XenoKit_BacTypeSortMode { get; set; }
        public bool XenoKit_SuppressErrorsToLogOnly { get; set; } = false;
        public bool XenoKit_DelayLoadingCMN { get; set; } = false;

        //Render Settings:
        public bool XenoKit_RimLightingEnabled { get; set; } = true;
        public bool XenoKit_EnableDynamicLighting { get; set; } = false;
        public int XenoKit_SuperSamplingFactor { get; set; } = 2;
        public int XenoKit_ShadowMapRes { get; set; } = 2048;
        public bool XenoKit_FullLowRez { get; set; } = false;
        public bool XenoKit_UseOutlinePostEffect { get; set; } = true;

        //Enums, which are not-serialized directly
        [DontSerialize]
        public BacTypeSortMode XenoKit_BacTypeSortModeEnum
        {
            get => (BacTypeSortMode)XenoKit_BacTypeSortMode;
            set => XenoKit_BacTypeSortMode = (int)value;
        }

        #endregion

        public static Settings Load(string path)
        {
            Settings settings = new Settings();
            SettingsFormat format = SettingsFormat.Load(path);

            format.DeserializeProps(settings);
            settings.settingsBinary = format;

            return settings;
        }

        public void Save(string path)
        {
            if (settingsBinary == null)
                settingsBinary = new SettingsFormat();

            settingsBinary.SerializeProps(this);
            File.WriteAllText(path, settingsBinary.Write());
        }

        public virtual void InitSettings()
        {
            //Get game dir
            if (string.IsNullOrWhiteSpace(GameDirectory) || !ValidGameDir)
            {
                GameDirectory = FindGameDirectory();
            }

            //Setup Undo
            if (UndoLimit < 0) UndoLimit = 0;
            if (UndoLimit > UndoManager.MaximumMaxCapacity) UndoLimit = UndoManager.MaximumMaxCapacity;

            UndoManager.Instance.SetCapacity(UndoLimit);

            //Allowed SuperSamplingFactors: 1, 2, 4 and 8
            if (XenoKit_SuperSamplingFactor != 1 && XenoKit_SuperSamplingFactor != 2 && XenoKit_SuperSamplingFactor != 4 && XenoKit_SuperSamplingFactor != 8)
            {
                XenoKit_SuperSamplingFactor = 1;
            }

            //Enable SSAA and dynamic lighting by default
            if (SettingsVersion == 2)
            {
                SettingsVersion = 3;
                XenoKit_SuperSamplingFactor = 2;
                XenoKit_EnableDynamicLighting = true;
            }

            //Theme
            InitTheme();
        }

        public virtual void ValidateSettings()
        {
            //EEPK Settings
            if (_textureReuse != EepkTextureReuse.Identical && _textureReuse != EepkTextureReuse.NameMatch)
                _textureReuse = EepkTextureReuse.Identical;

            if (_fileCleanUp != EepkFileCleanUp.Delete && _fileCleanUp != EepkFileCleanUp.Ignore && _fileCleanUp != EepkFileCleanUp.Prompt)
                _fileCleanUp = EepkFileCleanUp.Prompt;

            //XenoKit
            XenoKit_DelayedUpdateFrameInterval = MathHelpers.Clamp(1, 60, XenoKit_DelayedUpdateFrameInterval);
        }

        private string FindGameDirectory()
        {
            List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "O", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            foreach (var letter in alphabet)
            {
                string _path1 = String.Format(@"{0}:{1}Program Files (x86){1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);
                string _path2 = String.Format(@"{0}:{1}Games{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);
                string _path3 = String.Format(@"{0}:{1}Steam{1}steamapps{1}common{1}DB Xenoverse 2{1}bin{1}DBXV2.exe", letter, System.IO.Path.DirectorySeparatorChar);

                if (File.Exists(_path1))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path1));
                }
                else if (File.Exists(_path2))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path2));
                }
                else if (File.Exists(_path3))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(_path3));
                }
            }

            return null;
        }

        public AppTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        public void InitTheme(bool forceSet = false)
        {
            if (!DefaultThemeSet || forceSet)
            {
                //Check registry for the users Light/Dark mode preferences (Windows 10 only)
                var registryValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "1");

                if (registryValue != null)
                    if (registryValue.ToString() == "0")
                        _currentTheme = AppTheme.Dark;
                    else
                        _currentTheme = AppTheme.Light;

                NotifyPropertyChanged(nameof(UseDarkTheme));
                NotifyPropertyChanged(nameof(UseLightTheme));

                DefaultThemeSet = true;
            }
        }

        #region Enums
        private enum EepkFileCleanUp
        {
            Delete,
            Prompt,
            Ignore
        }

        private enum EepkTextureReuse
        {
            Identical,
            NameMatch
        }


        #endregion
    }

    #region Enums
    public enum BacTypeSortMode
    {
        Default = 0, //Type Order
        StartTime = 1,
    }

    public enum AppTheme
    {
        Light,
        Dark
    }

    public enum AppAccent
    {
        Red,
        Green,
        Blue,
        Purple,
        Orange,
        Lime,
        Emerald,
        Teal,
        Cyan,
        Cobalt,
        Indigo,
        Violet,
        Pink,
        Magenta,
        Crimson,
        Amber,
        Yellow,
        Brown,
        Olive,
        Steel,
        Mauve,
        Taupe,
        Sienna
    }
    #endregion

}
