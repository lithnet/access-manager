using System.ComponentModel;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotifiableEventPublisher
    {
        void Register(INotifyPropertyChanged item);
    }
}