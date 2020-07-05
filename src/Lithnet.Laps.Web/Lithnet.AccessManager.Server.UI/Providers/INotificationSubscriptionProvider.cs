using System.Collections.Generic;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotificationSubscriptionProvider
    {
        BindableCollection<SubscriptionViewModel> Subscriptions { get; }

        BindableCollection<SubscriptionViewModel> GetSubscriptions(IEnumerable<string> ids);

        void Rebuild();

        bool IsUnique(string name, string id);
    }
}