using EarTrumpet.DataModel;
using EarTrumpet.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace EarTrumpet.UI.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public event EventHandler Ready;
        public event EventHandler<FlyoutShowOptions> FlyoutShowRequested;
        public event EventHandler<DeviceViewModel> DefaultDeviceChanged;

        public ObservableCollection<DeviceViewModel> Devices { get; private set; }
        public DeviceViewModel DefaultDevice { get; private set; }
        public AudioDeviceKind DeviceKind => _deviceManager.DeviceKind;

        private readonly IAudioDeviceManager _deviceManager;
        private readonly Timer _peakMeterTimer;
        private bool _isFlyoutVisible;
        private bool _isFullWindowVisible;

        internal MainViewModel(IAudioDeviceManager deviceManager)
        {
            Devices = new ObservableCollection<DeviceViewModel>();

            _deviceManager = deviceManager;
            _deviceManager.DefaultChanged += DeviceManager_DefaultDeviceChanged;
            _deviceManager.Loaded += DeviceManager_Loaded;
            _deviceManager.Devices.CollectionChanged += Devices_CollectionChanged;
            Devices_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            _peakMeterTimer = new Timer(1000 / 30); // 30 fps
            _peakMeterTimer.AutoReset = true;
            _peakMeterTimer.Elapsed += PeakMeterTimer_Elapsed;

            BindHotkey();
        }

        private void BindHotkey()
        {
            HotkeyService.Pressed += HotkeyService_Pressed;

            var isPlayback = _deviceManager.DeviceKind == AudioDeviceKind.Playback;
            HotkeyService.Bind(
                isPlayback ? HotkeyService.HotkeyKind.PlaybackFlyout : HotkeyService.HotkeyKind.RecordingFlyout,
                isPlayback ? SettingsService.PlaybackFlyoutHotkey : SettingsService.RecordingFlyoutHotkey);
        }

        private void HotkeyService_Pressed(HotkeyService.HotkeyKind kind)
        {
            if (kind == HotkeyService.HotkeyKind.PlaybackFlyout && _deviceManager.DeviceKind == AudioDeviceKind.Playback ||
                kind == HotkeyService.HotkeyKind.RecordingFlyout && _deviceManager.DeviceKind == AudioDeviceKind.Recording)
            {
                OpenFlyout(FlyoutShowOptions.Keyboard);
            }
        }

        private void DeviceManager_Loaded(object sender, EventArgs e)
        {
            Ready?.Invoke(this, null);
        }

        private void DeviceManager_DefaultDeviceChanged(object sender, IAudioDevice e)
        {
            if (e == null)
            {
                DefaultDevice = null;
                DefaultDeviceChanged?.Invoke(this, DefaultDevice);
            }
            else
            {
                var dev = Devices.FirstOrDefault(d => d.Id == e.Id);
                if (dev != null)
                {
                    DefaultDevice = dev;
                    DefaultDeviceChanged?.Invoke(this, DefaultDevice);
                }
            }
        }

        private void AddDevice(IAudioDevice device)
        {
            var newDevice = new DeviceViewModel(_deviceManager, device);
            Devices.Add(newDevice);
        }

        private void Devices_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddDevice((IAudioDevice)e.NewItems[0]);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    var removed = ((IAudioDevice)e.OldItems[0]).Id;
                    var allExisting = Devices.FirstOrDefault(d => d.Id == removed);
                    if (allExisting != null)
                    {
                        Devices.Remove(allExisting);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Devices.Clear();
                    foreach (var device in _deviceManager.Devices)
                    {
                        AddDevice(device);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void PeakMeterTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // We're in the background so we need to use a snapshot.
            foreach (var device in Devices.ToArray())
            {
                device.UpdatePeakValueBackground();
            }

            App.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (var device in Devices)
                {
                    device.UpdatePeakValueForeground();
                }
            }));
        }

        public void MoveAppToDevice(IAppItemViewModel app, DeviceViewModel dev)
        {
            // Collect all matching apps on all devices.
            var apps = new List<IAppItemViewModel>();
            apps.Add(app);

            foreach (var device in Devices)
            {
                foreach (var deviceApp in device.Apps)
                {
                    if (deviceApp.DoesGroupWith(app))
                    {
                        if (!apps.Contains(deviceApp))
                        {
                            apps.Add(deviceApp);
                            break;
                        }
                    }
                }
            }

            foreach (var foundApp in apps)
            {
                MoveAppToDeviceInternal(foundApp, dev);
            }

            // Collect and move any hidden/moved sessions.
            _deviceManager.MoveHiddenAppsToDevice(app.AppId, dev?.Id);
        }

        private void MoveAppToDeviceInternal(IAppItemViewModel app, DeviceViewModel dev)
        {
            var searchId = dev?.Id;
            if (dev == null)
            {
                searchId = _deviceManager.Default.Id;
            }
            DeviceViewModel oldDevice = Devices.First(d => d.Apps.Contains(app));
            DeviceViewModel newDevice = Devices.First(d => searchId == d.Id);

            try
            {
                bool isLogicallyMovingDevices = (oldDevice != newDevice);

                var tempApp = new TemporaryAppItemViewModel(app, _deviceManager);

                app.MoveToDevice(dev?.Id, hide: isLogicallyMovingDevices);

                // Update the UI if the device logically changed places.
                if (isLogicallyMovingDevices)
                {
                    oldDevice.AppVirtuallyLeavingFromThisDevice(app);
                    newDevice.AppVirtuallMovingToThisDevice(tempApp);
                }
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }
        }

        private void StartOrStopPeakTimer()
        {
            _peakMeterTimer.Enabled = (_isFlyoutVisible | _isFullWindowVisible);
        }

        public void OnTrayFlyoutShown()
        {
            _isFlyoutVisible = true;
            StartOrStopPeakTimer();
        }

        public void OnTrayFlyoutHidden()
        {
            _isFlyoutVisible = false;
            StartOrStopPeakTimer();
        }

        public void OnFullWindowClosed()
        {
            _isFullWindowVisible = false;
            StartOrStopPeakTimer();
        }

        public void OnFullWindowOpened()
        {
            _isFullWindowVisible = true;
            StartOrStopPeakTimer();
        }

        public void OpenFlyout(FlyoutShowOptions options)
        {
            Trace.WriteLine($"MainViewModel OpenFlyout {options}");
            FlyoutShowRequested(this, options);
        }
    }
}
