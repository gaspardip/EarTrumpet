using EarTrumpet.DataModel;
using EarTrumpet.UI.Controls;
using EarTrumpet.UI.Helpers;
using EarTrumpet.UI.Services;
using EarTrumpet.UI.ViewModels;
using EarTrumpet.UI.Views;
using System.Diagnostics;
using System.Windows;

namespace EarTrumpet
{
    public partial class App
    {
        private TrayIcon _playbackTrayIcon;
        private FlyoutWindow _playbackFlyoutWindow;
        private TrayIcon _recordingTrayIcon;
        private FlyoutWindow _recordingFlyoutWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ErrorReportingService.Initialize();

            Trace.WriteLine("App Application_Startup");

            if (!SingleInstanceAppMutex.TakeExclusivity())
            {
                Trace.WriteLine("App Application_Startup TakeExclusivity failed");
                Current.Shutdown();
                return;
            }

            StartupUWPDialogDisplayService.ShowIfAppropriate();

            ((ThemeManager)Resources["ThemeManager"]).SetTheme(ThemeData.GetBrushData());

            var playbackDeviceManager = DataModelFactory.CreateAudioDeviceManager(AudioDeviceKind.Playback);
            DiagnosticsService.Advise(playbackDeviceManager);

            var playbackViewModel = new MainViewModel(playbackDeviceManager);
            playbackViewModel.Ready += PlaybackViewModel_Ready;

            _playbackFlyoutWindow = new FlyoutWindow(playbackViewModel, new FlyoutViewModel(playbackViewModel));
            _playbackTrayIcon = new TrayIcon(new TrayViewModel(playbackViewModel));

            if (IsMouthFeatureEnabled())
            {
                var recordingDeviceManager = DataModelFactory.CreateAudioDeviceManager(AudioDeviceKind.Recording);
                DiagnosticsService.Advise(recordingDeviceManager);

                var recordingViewModel = new MainViewModel(recordingDeviceManager);
                recordingViewModel.Ready += RecordingViewModel_Ready;

                _recordingFlyoutWindow = new FlyoutWindow(recordingViewModel, new FlyoutViewModel(recordingViewModel));
                _recordingTrayIcon = new TrayIcon(new TrayViewModel(recordingViewModel));
            }

            Trace.WriteLine($"App Application_Startup Exit");
        }

        private void PlaybackViewModel_Ready(object sender, System.EventArgs e)
        {
            Trace.WriteLine("App PlaybackViewModel_Ready");
            _playbackTrayIcon.Show();
        }

        private void RecordingViewModel_Ready(object sender, System.EventArgs e)
        {
            Trace.WriteLine("App RecordingViewModel_Ready");
            _recordingTrayIcon.Show();
        }

        public static bool IsMouthFeatureEnabled()
        {
            return true;
        }
    }
}
