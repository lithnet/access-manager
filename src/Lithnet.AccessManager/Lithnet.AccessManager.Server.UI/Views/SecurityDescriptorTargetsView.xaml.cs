using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for SecurityDescriptorTargetsView.xaml
    /// </summary>
    public partial class SecurityDescriptorTargetsView : UserControl
    {
        public SecurityDescriptorTargetsView()
        {
            InitializeComponent();
        }

        GridViewColumnHeader lastHeaderClicked = null;
        ListSortDirection lastDirection = ListSortDirection.Ascending;

        void listbox_Click(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader gridViewColumnHeader))
            {
                return;
            }

            ListSortDirection listSortDirection = ListSortDirection.Ascending;

            if (gridViewColumnHeader == lastHeaderClicked && lastDirection == ListSortDirection.Ascending)
            {
                listSortDirection = ListSortDirection.Descending;
            }

            SortListView(sender as ListView, gridViewColumnHeader, listSortDirection);

            lastHeaderClicked = gridViewColumnHeader;
            lastDirection = listSortDirection;
        }

        private void SortListView(ListView lv, GridViewColumnHeader ch, ListSortDirection dir)
        {
            if (lv == null)
            {
                return;
            }

            string propertyName = (ch.Column.DisplayMemberBinding as Binding)?.Path.Path;
            propertyName ??= ch.Column.Header as string;
          
            if (propertyName == null)
            {
                return;
            }

            ICollectionView defaultView = CollectionViewSource.GetDefaultView(lv.ItemsSource);
            defaultView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(propertyName, dir);
            defaultView.SortDescriptions.Add(sd);
            defaultView.Refresh();
        }
    }
}
