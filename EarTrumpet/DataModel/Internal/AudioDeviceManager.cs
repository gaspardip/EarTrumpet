﻿using EarTrumpet.DataModel.Internal.Services;
using EarTrumpet.Extensions;
using EarTrumpet.Interop;
using EarTrumpet.Interop.MMDeviceAPI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EarTrumpet.DataModel.Internal
{
    class AudioDeviceManager : IMMNotificationClient, IAudioDeviceManager
    {
        public event EventHandler<IAudioDevice> DefaultChanged;
        public event EventHandler Loaded;

        public ObservableCollection<IAudioDevice> Devices => _devices;

        private static IPolicyConfig s_PolicyConfigClient = null;

        private IMMDeviceEnumerator _enumerator;
        private IAudioDevice _defaultDevice;
        private ObservableCollection<IAudioDevice> _devices = new ObservableCollection<IAudioDevice>();
        private Dispatcher _dispatcher;
        private EDataFlow _flow;
        private AudioPolicyConfigService _configService;

        public AudioDeviceManager(Dispatcher dispatcher, AudioDeviceKind flow)
        {
            Trace.WriteLine("AudioDeviceManager Create");

            _dispatcher = dispatcher;
            _flow = flow == AudioDeviceKind.Playback ? EDataFlow.eRender : EDataFlow.eCapture;

            _configService = new AudioPolicyConfigService(flow);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                    _enumerator.RegisterEndpointNotificationCallback(this);

                    var devices = _enumerator.EnumAudioEndpoints(_flow, DeviceState.ACTIVE);
                    uint deviceCount = devices.GetCount();
                    for (uint i = 0; i < deviceCount; i++)
                    {
                        ((IMMNotificationClient)this).OnDeviceAdded(devices.Item(i).GetId());
                    }

                    // Trigger default logic to register for volume change
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        QueryDefaultDevice();
                        Loaded?.Invoke(this, null);
                    }));
                }
                catch (Exception ex) when (ex.Is(Error.AUDCLNT_E_DEVICE_INVALIDATED))
                {
                    // Expected in some cases.
                }
            });

            Trace.WriteLine("AudioDeviceManager Create Exit");
        }

        ~AudioDeviceManager()
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }
        }

        private void QueryDefaultDevice()
        {
            Trace.WriteLine("AudioDeviceManager QueryDefaultDevice");
            IMMDevice device = null;
            try
            {
                device = _enumerator.GetDefaultAudioEndpoint(_flow, ERole.eMultimedia);
            }
            catch (Exception ex) when (ex.Is(Error.ERROR_NOT_FOUND))
            {
                // Expected.
            }

            string newDeviceId = device?.GetId();
            var currentDeviceId = _defaultDevice?.Id;
            if (currentDeviceId != newDeviceId)
            {
                FindDevice(newDeviceId, out _defaultDevice);

                DefaultChanged?.Invoke(this, _defaultDevice);
            }
        }

        public IAudioDevice Default
        {
            get => _defaultDevice;
            set
            {
                if (_defaultDevice == null || value.Id != _defaultDevice.Id)
                {
                    SetDefaultDevice(value);
                }
            }
        }

        public AudioDeviceKind DeviceKind
        {
            get => _flow == EDataFlow.eRender ? AudioDeviceKind.Playback : AudioDeviceKind.Recording;
        }

        private void SetDefaultDevice(IAudioDevice device)
        {
            Trace.WriteLine($"AudioDeviceManager SetDefaultDevice {device.Id}");

            if (s_PolicyConfigClient == null)
            {
                s_PolicyConfigClient = (IPolicyConfig)new PolicyConfigClient();
            }

            // Racing with the system, the device may not be valid anymore.
            try
            {
                s_PolicyConfigClient.SetDefaultEndpoint(device.Id, ERole.eMultimedia);
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }
        }

        private bool FindDevice(string deviceId, out IAudioDevice found)
        {
            if (deviceId == null)
            {
                found = null;
                return false;
            }

            found = _devices.ToArray().FirstOrDefault(d => d.Id == deviceId);
            return found != null;
        }

        public void MoveHiddenAppsToDevice(string appId, string id)
        {
            foreach (var device in _devices)
            {
                device.MoveHiddenAppsToDevice(appId, id);
            }
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId)
        {
            Trace.WriteLine($"AudioDeviceManager OnDeviceAdded {pwstrDeviceId}");

            if (!FindDevice(pwstrDeviceId, out IAudioDevice unused))
            {
                try
                {
                    IMMDevice device = _enumerator.GetDevice(pwstrDeviceId);
                    if (((IMMEndpoint)device).GetDataFlow() == _flow)
                    {
                        var newDevice = new AudioDevice(device, this);

                        _dispatcher.BeginInvoke((Action)(() =>
                        {
                            // We must check again on the UI thread to avoid adding a duplicate device.
                            if (!FindDevice(pwstrDeviceId, out IAudioDevice unused1))
                            {
                                _devices.Add(newDevice);
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    // We catch Exception here because IMMDevice::Activate can return E_POINTER/NullReferenceException, as well as other expcetions listed here:
                    // https://docs.microsoft.com/en-us/dotnet/framework/interop/how-to-map-hresults-and-exceptions
                    AppTrace.LogWarning(ex);
                }
            }
        }

        void IMMNotificationClient.OnDeviceRemoved(string pwstrDeviceId)
        {
            Trace.WriteLine($"AudioDeviceManager OnDeviceRemoved {pwstrDeviceId}");

            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (FindDevice(pwstrDeviceId, out IAudioDevice dev))
                {
                    _devices.Remove(dev);
                }
            }));
        }

        void IMMNotificationClient.OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId)
        {
            Trace.WriteLine($"AudioDeviceManager OnDefaultDeviceChanged {pwstrDefaultDeviceId}");

            _dispatcher.BeginInvoke((Action)(() =>
            {
                QueryDefaultDevice();
            }));
        }

        void IMMNotificationClient.OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState)
        {
            Trace.WriteLine($"AudioDeviceManager OnDeviceStateChanged {pwstrDeviceId} {dwNewState}");
            switch (dwNewState)
            {
                case DeviceState.ACTIVE:
                    ((IMMNotificationClient)this).OnDeviceAdded(pwstrDeviceId);
                    break;
                case DeviceState.DISABLED:
                case DeviceState.NOTPRESENT:
                case DeviceState.UNPLUGGED:
                    ((IMMNotificationClient)this).OnDeviceRemoved(pwstrDeviceId);
                    break;
                default:
                    Trace.TraceError($"Unknown DEVICE_STATE: {dwNewState}");
                    break;
            }
        }

        void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PROPERTYKEY key)
        {
            Trace.WriteLine($"AudioDeviceManager OnPropertyValueChanged {pwstrDeviceId} {key.fmtid}{key.pid}");
            if (FindDevice(pwstrDeviceId, out IAudioDevice dev))
            {
                if (PropertyKeys.PKEY_AudioEndPoint_Interface.Equals(key))
                {
                    // We're racing with the system, the device may not be resolvable anymore.
                    try
                    {
                        ((AudioDevice)dev).DevicePropertiesChanged(_enumerator.GetDevice(dev.Id));
                    }
                    catch (Exception ex)
                    {
                        AppTrace.LogWarning(ex);
                    }
                }
            }
        }

        public string GetDefaultEndPoint(int processId)
        {
            return _configService.GetDefaultEndPoint(processId);
        }

        public void SetDefaultEndPoint(string id, int processId)
        {
            _configService.SetDefaultEndPoint(id, processId);
        }
    }
}
