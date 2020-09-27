using System.ComponentModel;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotifyModelChangedEventPublisher
    {
        void Register(INotifyPropertyChanged item);

        void Pause();

        void Unpause();
    }
}