using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Aurender.Core.Player
{

    public interface IVolumeController : INotifyPropertyChanged
    {
        String CurrentVolume { get; }
        IList<String> AvailbleSources { get; }
        String CurrentSource { get; }

        bool IsPhasePlus { get; }

        VolumeControllerCapability Capabilty { get; }
        
        Task LoadVolumeStatus();
        Task VolumeUp();
        Task VolumeDown();
        Task Mute(bool on);

        Task ToggleMute();
        Task SetPhase(bool plus);
        Task TogglePhase();

        Task SelectSource(String source);
    }

    [Flags]
    public enum VolumeControllerCapability
    {
        NoControl = 1 << 0,
        Volume = 1 << 1,
        Mute = 1 << 2,
        Source = 1 << 3,
        Phase = 1 << 4,
        PhaseToggle = 1 << 5,
        Power = 1 << 6,
    }
}
namespace Aurender.Core.Player.VolumeController
{
}