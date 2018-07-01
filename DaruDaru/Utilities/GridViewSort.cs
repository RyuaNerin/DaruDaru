using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DaruDaru.Utilities
{
    internal enum SortDirection
    {
        None = -1,
        Ascending = 0,
        Descending = 1
    }

    internal static class GridViewSort
    {
        private static SortDirection GetSortDirection(DependencyObject obj)
            => (SortDirection)obj.GetValue(SortDirectionProperty);
        private static void SetSortDirection(DependencyObject obj, SortDirection value)
            => obj.SetValue(SortDirectionProperty, value);
        private static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.RegisterAttached("SortDirection", typeof(SortDirection), typeof(GridViewSort), new UIPropertyMetadata(SortDirection.None));

        public static bool GetAutoSort(DependencyObject obj)
            => (bool)obj.GetValue(AutoSortProperty);
        public static void SetAutoSort(DependencyObject obj, bool value)
            => obj.SetValue(AutoSortProperty, value);
        public static readonly DependencyProperty AutoSortProperty =
            DependencyProperty.RegisterAttached("AutoSort", typeof(bool), typeof(GridViewSort), new UIPropertyMetadata(false, AutoSortPropertyChanged));
        private static void AutoSortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;
                if (oldValue == newValue)
                    return;

                if (newValue)
                    listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                else
                    listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
            }
        }

        public static string GetPropertyName(DependencyObject obj)
            => (string)obj.GetValue(PropertyNameProperty);
        public static void SetPropertyName(DependencyObject obj, string value)
            => obj.SetValue(PropertyNameProperty, value);
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(GridViewSort), new UIPropertyMetadata(null));

        private static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj)
            => (GridViewColumnHeader)obj.GetValue(SortedColumnHeaderProperty);
        private static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value)
            => obj.SetValue(SortedColumnHeaderProperty, value);        
        private static readonly DependencyProperty SortedColumnHeaderProperty =
            DependencyProperty.RegisterAttached("SortedColumnHeader", typeof(GridViewColumnHeader), typeof(GridViewSort), new UIPropertyMetadata(null));
        
        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked != null && headerClicked.Column != null)
            {
                var propertyName = GetPropertyName(headerClicked.Column);

                if (!string.IsNullOrEmpty(propertyName))
                {
                    var listView = GetAncestor<ListView>(headerClicked);
                    if (listView != null)
                    {
                        if (GetAutoSort(listView))
                            ApplySort(listView, headerClicked, propertyName);
                    }
                }
            }
        }        

        public static T GetAncestor<T>(DependencyObject item)
            where T : DependencyObject
        {
            do
            {
                item = VisualTreeHelper.GetParent(item);
            }
            while (!(item is T));

            return item as T;
        }

        public static void ApplySort(ListView listView, GridViewColumnHeader column, string propertyName)
        {
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);

            var direction = GetSortDirection(listView);
            if (direction == SortDirection.None)
                direction = SortDirection.Ascending;
            else if (direction == SortDirection.Ascending)
                direction = SortDirection.Descending;
            else
                direction = SortDirection.Ascending;

            var curColumn = GetSortedColumnHeader(listView);
            if (curColumn != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(curColumn);
                var adorner = layer.GetAdorners(curColumn)?.FirstOrDefault(e => e is SortGlyphAdorner);

                if (adorner != null)
                    layer.Remove(adorner);
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                using (view.DeferRefresh())
                {
                    view.CustomSort = new CustomSorter(propertyName, (ListSortDirection)direction);

                    SetSortedColumnHeader(listView, column);

                    SetSortDirection(listView, direction);

                    AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(column);
                    adornerLayer.Add(new SortGlyphAdorner(column, direction));
                }
            }
        }
        
        private class SortGlyphAdorner : Adorner
        {
            private readonly GridViewColumnHeader m_column;
            private readonly SortDirection m_direction;

            public SortGlyphAdorner(GridViewColumnHeader column, SortDirection direction)
                : base(column)
            {
                this.m_column = column;
                this.m_direction = direction;
            }

            private static readonly Pen GlpyhPen = new Pen(Brushes.Gray, 1.0);

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);
                
                double x1 = (this.m_column.ActualWidth - 10) / 2;
                double x2 = x1 + 5;
                double x3 = x1 + 10;
                double y1 = 2;
                double y2 = y1 + 3;

                if (this.m_direction == SortDirection.Ascending)
                {
                    drawingContext.DrawLine(GlpyhPen, new Point(x1, y2), new Point(x2, y1));
                    drawingContext.DrawLine(GlpyhPen,                    new Point(x2, y1), new Point(x3, y2));
                }
                else if (this.m_direction == SortDirection.Descending)
                {
                    drawingContext.DrawLine(GlpyhPen, new Point(x1, y1), new Point(x2, y2));
                    drawingContext.DrawLine(GlpyhPen,                    new Point(x2, y2), new Point(x3, y1));
                }
            }
        }
        
        private class CustomSorter : IComparer
        {
            public CustomSorter(string propertyName, ListSortDirection direction)
            {
                this.m_propertyName = propertyName;
                this.m_direction = direction;
            }

            private readonly string m_propertyName;
            private readonly ListSortDirection m_direction;

            public int Compare(object x, object y)
            {
                var xx = x.GetType().GetProperty(this.m_propertyName).GetValue(x);
                var yy = y.GetType().GetProperty(this.m_propertyName).GetValue(y);

                int r;
                if (xx is string xxx && yy is string yyy)
                    r = CompareTo(xxx, yyy);
                else
                    r = Comparer.Default.Compare(xx, yy);

                return this.m_direction == ListSortDirection.Ascending ? r : r * -1;
            }


            public static int CompareTo(string x, string y)
            {
                int xindex, yindex;
                int xlen, ylen;
                float xint, yint;

                int i;
                int k;
                int c;

                xindex = yindex = 0;
                while (xindex < x.Length && yindex < y.Length)
                {
                    xlen = Cut(x, xindex);
                    ylen = Cut(y, yindex);

                    if (xlen == 0 || ylen == 0)
                        c = xlen.CompareTo(ylen);

                    else if ((char.IsDigit(x[xindex]) || x[xindex] == '.') &&
                             (char.IsDigit(y[yindex]) || y[yindex] == '.') &&
                             float.TryParse(x.Substring(xindex, xlen), out xint) &&
                             float.TryParse(y.Substring(yindex, ylen), out yint))
                        c = xint.CompareTo(yint);

                    else
                    {
                        k = Math.Min(xlen, ylen);
                        c = 0;
                        for (i = 0; i < k; ++i)
                        {
                            if (x[xindex + i] != y[yindex + i])
                            {
                                c = x[xindex + i].CompareTo(y[yindex + i]);
                                break;
                            }
                        }

                        if (c == 0)
                            c = xlen.CompareTo(ylen);

                    }

                    xindex += xlen;
                    yindex += ylen;

                    if (c != 0)
                        return c;
                }

                return x.Length - y.Length;
            }

            private static int Cut(string x, int xindex)
            {
                var isFloat = char.IsDigit(x[xindex]);

                int nindex = xindex + 1;
                while (nindex < x.Length)
                {
                    if (isFloat != (char.IsDigit(x[nindex]) || x[nindex] == '.'))
                        break;

                    nindex++;
                }

                return Math.Min(nindex, x.Length) - xindex;
            }
        }
    }
}
