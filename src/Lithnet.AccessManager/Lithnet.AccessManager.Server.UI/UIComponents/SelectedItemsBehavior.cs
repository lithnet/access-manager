using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Telerik.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectedItemsBehavior : Behavior<RadGridView>
    {
        private bool collectionChangedSuspended;

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>
        /// Override this to hook up functionality to the AssociatedObject.
        /// </remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectedItems.CollectionChanged += this.GridSelectedItemsCollectionChanged;
        }

        /// <summary>
        /// Getter/Setter for DependencyProperty, bound to the DataContext's SelectedItems ObservableCollection
        /// </summary>
        public INotifyCollectionChanged SelectedItems
        {
            get => (INotifyCollectionChanged)this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        /// <summary>
        /// Dependency Property "SelectedItems" is target of binding to DataContext's SelectedItems
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(INotifyCollectionChanged), typeof(SelectedItemsBehavior), new PropertyMetadata(OnSelectedItemsPropertyChanged));

        /// <summary>
        /// PropertyChanged handler for DependencyProperty "SelectedItems"
        /// </summary>
        private static void OnSelectedItemsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            INotifyCollectionChanged collection = args.NewValue as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged += ((SelectedItemsBehavior)target).ContextSelectedItemsCollectionChanged;
            }
        }

        /// <summary>
        /// Will be called, when the Network's SelectedItems collection changes
        /// </summary>
        private void ContextSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.collectionChangedSuspended)
            {
                return;
            }

            this.collectionChangedSuspended = true;

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    this.AssociatedObject.SelectedItems.Add(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    this.AssociatedObject.SelectedItems.Remove(item);
                }
            }

            this.collectionChangedSuspended = false;
        }

        /// <summary>
        /// Will be called when the GridView's SelectedItems collection changes
        /// </summary>
        private void GridSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.collectionChangedSuspended)
            {
                return;
            }

            this.collectionChangedSuspended = true;

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    ((IList)this.SelectedItems).Add(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    ((IList)this.SelectedItems).Remove(item);
                }
            }

            this.collectionChangedSuspended = false;
        }
    }
}
