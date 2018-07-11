using EarTrumpet.Extensions;
using EarTrumpet.UI.Helpers;
using EarTrumpet.UI.Services;
using System;
using System.Windows.Input;
using Windows.ApplicationModel;

namespace EarTrumpet.UI.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        SettingsService.HotkeyData _playbackFlyoutHotkey;
        internal SettingsService.HotkeyData PlaybackFlyoutHotkey
        {
            get => _playbackFlyoutHotkey;
            set
            {
                _playbackFlyoutHotkey = value;
                SettingsService.PlaybackFlyoutHotkey = _playbackFlyoutHotkey;
                RaisePropertyChanged(nameof(PlaybackFlyoutHotkey));
                RaisePropertyChanged(nameof(PlaybackFlyoutHotkeyText));
            }
        }

        public string PlaybackFlyoutHotkeyText => _playbackFlyoutHotkey.ToString();
        public string DefaultPlaybackFlyoutHotKey => SettingsService.s_defaultPlaybackHotkey.ToString();

        SettingsService.HotkeyData _recordingFlyoutHotkey;
        internal SettingsService.HotkeyData RecordingFlyoutHotkey
        {
            get => _recordingFlyoutHotkey;
            set
            {
                _recordingFlyoutHotkey = value;
                SettingsService.RecordingFlyoutHotkey = _recordingFlyoutHotkey;
                RaisePropertyChanged(nameof(RecordingFlyoutHotkey));
                RaisePropertyChanged(nameof(RecordingFlyoutHotkeyText));
            }
        }

        public string RecordingFlyoutHotkeyText => _recordingFlyoutHotkey.ToString();
        public string DefaultRecordingFlyoutHotKey => SettingsService.s_defaultRecordingHotkey.ToString();

        public RelayCommand OpenDiagnosticsCommand { get; }
        public RelayCommand OpenAboutCommand { get; }
        public RelayCommand OpenFeedbackCommand { get; }

        public bool UseLegacyIcon
        {
            get => SettingsService.UseLegacyIcon;
            set => SettingsService.UseLegacyIcon = value;
        }

        public bool IsMouthTrumpetEnabled
        {
            get => SettingsService.IsMouthTrumpetEnabled;
            set => SettingsService.IsMouthTrumpetEnabled = value;
        }

        public string AboutText { get; private set; }

        internal SettingsViewModel()
        {
            PlaybackFlyoutHotkey = SettingsService.PlaybackFlyoutHotkey;
            RecordingFlyoutHotkey = SettingsService.RecordingFlyoutHotkey;
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
