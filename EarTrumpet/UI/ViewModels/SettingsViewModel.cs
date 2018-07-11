using EarTrumpet.Extensions;
using EarTrumpet.UI.Helpers;
using EarTrumpet.UI.Services;
using System;
using System.Diagnostics;
using System.Windows.Input;
using Windows.ApplicationModel;

namespace EarTrumpet.UI.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        SettingsService.HotkeyData _hotkey;
        internal SettingsService.HotkeyData Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                SettingsService.PlaybackFlyoutHotkey = _hotkey;
                RaisePropertyChanged(nameof(Hotkey));
                RaisePropertyChanged(nameof(HotkeyText));
            }
        }

        public string HotkeyText => _hotkey.ToString();
        public string DefaultHotKey => SettingsService.s_defaultPlaybackHotkey.ToString();
        public RelayCommand OpenDiagnosticsCommand { get; }
        public RelayCommand OpenAboutCommand { get; }
        public RelayCommand OpenFeedbackCommand { get; }

        public bool UseLegacyIcon
        {
            get => SettingsService.UseLegacyIcon;
            set => SettingsService.UseLegacyIcon = value;
        }

        public string AboutText { get; private set; }

        internal SettingsViewModel()
        {
            Hotkey = SettingsService.PlaybackFlyoutHotkey;
            OpenAboutCommand = new RelayCommand(OpenAbout);
            OpenDiagnosticsCommand = new RelayCommand(OpenDiagnostics);
            OpenFeedbackCommand = new RelayCommand(FeedbackService.OpenFeedbackHub);

            string aboutFormat = "EarTrumpet {0}";
            if (App.Current.HasIdentity())
            {
                AboutText = string.Format(aboutFormat, Package.Current.Id.Version.ToVersionString());
            }
            else
            {
                AboutText = string.Format(aboutFormat, "0.0.0.0");
            }
        }

        private void OpenDiagnostics()
        {
            if(Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                throw new Exception("This is an intentional crash.");
            }

            DiagnosticsService.DumpAndShowData();
        }

        private void OpenAbout()
        {
            using (ProcessHelper.StartNoThrowAndLogWarning("https://github.com/File-New-Project/EarTrumpet")) { }
        }
    }
}
