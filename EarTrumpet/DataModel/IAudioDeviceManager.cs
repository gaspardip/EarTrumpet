using System;
using System.Collections.ObjectModel;

namespace EarTrumpet.DataModel
{
    interface IAudioDeviceManager
    {
        event EventHandler<IAudioDevice> DefaultChanged;
        event EventHandler Loaded;

        IAudioDevice Default { get; set; }
        AudioDeviceKind DeviceKind { get; }
        ObservableCollection<IAudioDevice> Devices { get; }

        void MoveHiddenAppsToDevice(string appId, string id);
        string GetDefaultEndPoint(int processId);
        void SetDefaultEndPoint(string id, int processId);
    }
}