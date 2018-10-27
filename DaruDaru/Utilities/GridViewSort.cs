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
    internal static class GridViewSort
    {
        private static ListSortDirection GetSortDirection(DependencyObject obj)
            => (ListSortDirection)obj.GetValue(SortDirectionProperty);
        private static void SetSortDirection(DependencyObject obj, ListSortDirection value)
            => obj.SetValue(SortDirectionProperty, value);
        private static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.RegisterAttached("SortDirection", typeof(ListSortDirection), typeof(GridViewSort), new UIPropertyMetadata(ListSortDirection.Ascending));

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
            while (item != null)
            {
                item = VisualTreeHelper.GetParent(item);
                if (item is T t)
                    return t;
            }
            return null;
        }

        public static void ApplySort(ListView listView, GridViewColumnHeader column, string propertyName)
        {
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);

            var direction = GetSortDirection(listView);

            var curColumn = GetSortedColumnHeader(listView);
            if (curColumn != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(curColumn);
                var adorner = layer.GetAdorners(curColumn)?.FirstOrDefault(e => e is SortGlyphAdorner);

                if (adorner != null)
                    layer.Remove(adorner);
            }

            if (curColumn == column)
            {
                if (direction == ListSortDirection.Ascending)
                    direction = ListSortDirection.Descending;
                else
                    direction = ListSortDirection.Ascending;
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
            private readonly ListSortDirection m_direction;

            public SortGlyphAdorner(GridViewColumnHeader column, ListSortDirection direction)
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

                if (this.m_direction == ListSortDirection.Ascending)
                {
                    drawingContext.DrawLine(GlpyhPen, new Point(x1, y2), new Point(x2, y1));
                    drawingContext.DrawLine(GlpyhPen,                    new Point(x2, y1), new Point(x3, y2));
                }
                else if (this.m_direction == ListSortDirection.Descending)
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
                this.m_direction = direction == ListSortDirection.Ascending ? 1 : -1;
            }

            private readonly string m_propertyName;
            private readonly int m_direction;

            public int Compare(object x, object y)
            {
                var xx = x.GetType().GetProperty(this.m_propertyName).GetValue(x);
                var yy = y.GetType().GetProperty(this.m_propertyName).GetValue(y);

                int r;
                if (xx is string xxx && yy is string yyy)
                    r = CompareTo(xxx, yyy);
                else
                    r = Comparer.Default.Compare(xx, yy);

                return r * this.m_direction;
            }
            
            public static int CompareTo(string x, string y)
            {
                // 대소문자 구분 안함
                x = x.ToUpper();
                y = y.ToUpper();

                int xindex, yindex;
                int xlen, ylen;
                float xint, yint;

                bool xIsNum , yIsNum;
                bool xIsNumb, yIsNumb;
                bool xb, yb;
                bool xl, yl;

                int i;
                int k;
                int c;

                xIsNumb = yIsNumb = false;

                xindex = yindex = 0;
                while (xindex < x.Length && yindex < y.Length)
                {
                    xlen = GetPartLength(x, xindex, out xIsNum);
                    ylen = GetPartLength(y, yindex, out yIsNum);

                    if (xlen == 0 || ylen == 0)
                        c = xlen.CompareTo(ylen);

                    else if (xIsNum && yIsNum)
                    {
                        xint = float.Parse(x.Substring(xindex, xlen));
                        yint = float.Parse(y.Substring(yindex, ylen));
                        c = xint.CompareTo(yint);
                    }
                    else
                    {
                        // ex)
                        // 만화 1화
                        // 만화 1-1화
                        if (!xIsNum  && !yIsNum &&
                             xIsNumb &&  yIsNumb)
                        {
                            xb = x[xindex] == '-';
                            yb = y[yindex] == '-';

                            if ( xb && !yb) return  1;
                            if (!xb &&  yb) return -1;
                        }

                        // ex)
                        // [단편] 만화
                        // 만화
                        if (x[xindex] < 128 && y[yindex] < 128)
                        {
                            xl = char.IsLetterOrDigit(x[xindex]);
                            yl = char.IsLetterOrDigit(y[yindex]);

                            if ( xl && !yl) return  1;
                            if (!xl &&  yl) return -1;
                        }

                        k = Math.Min(xlen, ylen);
                        for (i = 0; i < k; ++i)
                            if (x[xindex + i] != y[yindex + i])
                                return x[xindex + i].CompareTo(y[yindex + i]);

                        c = xlen.CompareTo(ylen);
                    }

                    if (c != 0)
                        return c;

                    xindex += xlen;
                    yindex += ylen;

                    xIsNumb = xIsNum;
                    yIsNumb = yIsNum;
                }

                return x.Length - y.Length;
            }

            static int GetPartLength(string x, int startIndex, out bool xIsNumber)
            {
                xIsNumber = char.IsDigit(x[startIndex]);

                var i = startIndex;

                if (xIsNumber)
                {
                    while (++i < x.Length)
                        if (!char.IsDigit(x[i]) && x[i] != '.')
                            return i - startIndex;
                }
                else
                {
                    while (++i < x.Length)
                        if (char.IsDigit(x[i]))
                            return i - startIndex;
                }

                return x.Length - startIndex;
            }
        }
    }
}
