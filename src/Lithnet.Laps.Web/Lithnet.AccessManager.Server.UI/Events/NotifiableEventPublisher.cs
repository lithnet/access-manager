using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotifiableEventPublisher : INotifiableEventPublisher
    {
        private readonly IEventAggregator eventAggregator;

        public NotifiableEventPublisher(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public void Register(INotifyPropertyChanged item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item.PropertyChanged += Item_PropertyChanged;

            foreach (var property in item.GetType().GetProperties())
            {
                if (typeof(INotifyCollectionChanged).IsAssignableFrom(property.PropertyType))
                {
                    if (Attribute.IsDefined(property, typeof(NotifiableCollectionAttribute)))
                    {
                        var v = property.GetValue(item) as INotifyCollectionChanged;
                        v.CollectionChanged += V_CollectionChanged;
                    }
                }
            }
        }

        private void V_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.eventAggregator.Publish(new ModelChangedEvent(sender, "Collection"));
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Type t = sender.GetType();
            PropertyInfo pi = t.GetProperty(e.PropertyName);

            if (Attribute.IsDefined(pi, typeof(NotifiablePropertyAttribute)))
            {
                this.eventAggregator.Publish(new ModelChangedEvent(sender, e.PropertyName));
            }
        }
    }
}
