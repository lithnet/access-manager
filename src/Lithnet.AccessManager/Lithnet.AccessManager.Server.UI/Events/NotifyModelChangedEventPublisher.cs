using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotifyModelChangedEventPublisher : INotifyModelChangedEventPublisher
    {
        private readonly IEventAggregator eventAggregator;

        private bool paused;

        public NotifyModelChangedEventPublisher(IEventAggregator eventAggregator)
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

            foreach (PropertyInfo property in item.GetType().GetProperties())
            {
                if (typeof(INotifyCollectionChanged).IsAssignableFrom(property.PropertyType))
                {
                    if (Attribute.IsDefined(property, typeof(NotifyModelChangedCollectionAttribute)))
                    {
                        NotifyModelChangedCollectionAttribute attribute = (NotifyModelChangedCollectionAttribute)Attribute.GetCustomAttribute(property, typeof(NotifyModelChangedCollectionAttribute));

                        if (property.GetValue(item) is INotifyCollectionChanged v)
                        {
                            v.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) { V_CollectionChanged(sender, e, attribute?.RequiresServiceRestart ?? false); };
                        }
                    }
                }
            }
        }

        public void Pause()
        {
            this.paused = true;
        }

        public void Unpause()
        {
            this.paused = false;
        }

        private void V_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e, bool restartService)
        {
            if (paused)
            {
                return;
            }

            this.eventAggregator.Publish(new ModelChangedEvent(sender, "Collection", restartService));
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (paused)
            {
                return;
            }

            Type t = sender.GetType();
            PropertyInfo pi = t.GetProperty(e.PropertyName);

            if (pi != null)
            {
                if (Attribute.IsDefined(pi, typeof(NotifyModelChangedPropertyAttribute)))
                {
                    NotifyModelChangedPropertyAttribute attribute = (NotifyModelChangedPropertyAttribute)Attribute.GetCustomAttribute(pi, typeof(NotifyModelChangedPropertyAttribute));   
                    
                    this.eventAggregator.Publish(new ModelChangedEvent(sender, e.PropertyName, attribute?.RequiresServiceRestart ?? false));
                }
            }
        }
    }
}
