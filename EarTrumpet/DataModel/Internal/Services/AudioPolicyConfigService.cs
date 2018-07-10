using EarTrumpet.Interop;
using EarTrumpet.Interop.MMDeviceAPI;
using System;
using System.Diagnostics;

namespace EarTrumpet.DataModel.Internal.Services
{
    class AudioPolicyConfigService
    {
        const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
        const string DEVINTERFACE_AUDIO_CAPTURE = "#{2eef81be-33fa-4800-9670-1cd474972c3f}";
        const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";

        private IAudioPolicyConfigFactory _policyConfig;
        private EDataFlow _flow;

        public AudioPolicyConfigService(AudioDeviceKind kind)
        {
            Guid iid = typeof(IAudioPolicyConfigFactory).GUID;
            Combase.RoGetActivationFactory("Windows.Media.Internal.AudioPolicyConfig", ref iid, out object factory);
            _policyConfig = (IAudioPolicyConfigFactory)factory;

            _flow = kind == AudioDeviceKind.Playback ? EDataFlow.eRender : EDataFlow.eCapture;
        }

        private string GenerateDeviceId(string deviceId)
        {
            return $"{MMDEVAPI_TOKEN}{deviceId}{(_flow == EDataFlow.eRender ? DEVINTERFACE_AUDIO_RENDER : DEVINTERFACE_AUDIO_CAPTURE)}";
        }

        private string UnpackDeviceId(string deviceId)
        {
            var endToken = (_flow == EDataFlow.eRender ? DEVINTERFACE_AUDIO_RENDER : DEVINTERFACE_AUDIO_CAPTURE);
            if (deviceId.StartsWith(MMDEVAPI_TOKEN)) deviceId = deviceId.Remove(0, MMDEVAPI_TOKEN.Length);
            if (deviceId.EndsWith(endToken)) deviceId = deviceId.Remove(deviceId.Length - endToken.Length);
            return deviceId;
        }

        public void SetDefaultEndPoint(string deviceId, int processId)
        {
            Trace.WriteLine($"AudioPolicyConfigService SetDefaultEndPoint {deviceId} {processId}");
            try
            {
                IntPtr hstring = IntPtr.Zero;

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    var str = GenerateDeviceId(deviceId);
                    Combase.WindowsCreateString(str, (uint)str.Length, out hstring);
                }

                _policyConfig.SetPersistedDefaultAudioEndpoint((uint)processId, _flow, ERole.eMultimedia, hstring);
                _policyConfig.SetPersistedDefaultAudioEndpoint((uint)processId, _flow, ERole.eConsole, hstring);
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }
        }

        public string GetDefaultEndPoint(int processId)
        {
            try
            {
                _policyConfig.GetPersistedDefaultAudioEndpoint((uint)processId, EDataFlow.eRender, ERole.eMultimedia | ERole.eConsole, out string deviceId);
                return UnpackDeviceId(deviceId);
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }

            return null;
        }
    }
}
