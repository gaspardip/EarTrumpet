﻿using System.Collections.ObjectModel;

namespace EarTrumpet.DataModel
{
    interface IAudioDevice : IStreamWithVolumeControl
    {
        IAudioDeviceManager Parent { get; }
        string DisplayName { get; }

        ObservableCollection<IAudioDeviceSession> Groups { get; }

        void UpdatePeakValueBackground();
        void UnhideSessionsForProcessId(int processId);
        void MoveHiddenAppsToDevice(string appId, string id);
    }
}