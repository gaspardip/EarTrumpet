using EarTrumpet.DataModel.Internal;
using System.Windows.Threading;

namespace EarTrumpet.DataModel
{
    class DataModelFactory
    {
        public static IAudioDeviceManager CreateAudioDeviceManager(AudioDeviceKind kind)
        {
            return new AudioDeviceManager(Dispatcher.CurrentDispatcher, kind);
        }
    }
}
