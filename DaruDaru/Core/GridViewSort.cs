using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DaruDaru.Core
{
    internal enum SortDirection
    {
        None = -1,
        Ascending = 0,
        Descending = 1
    }

    internal static class GridViewSort
    {
        public static SortDirection GetSortDirection(DependencyObject obj)
            => (SortDirection)obj.GetValue(SortDirectionProperty);
        public static void SetSortDirection(DependencyObject obj, SortDirection value)
            => obj.SetValue(SortDirectionProperty, value);
        public static readonly DependencyProperty SortDirectionProperty =
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
                            ApplySort(listView.Items, propertyName, listView, headerClicked);
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

        public static void ApplySort(ICollectionView view, string propertyName, ListView listView, GridViewColumnHeader column)
        {
            var direction = GetSortDirection(listView);
            if (direction == SortDirection.None)
                direction = SortDirection.Ascending;
            else if (direction == SortDirection.Ascending)
                direction = SortDirection.Descending;
            else
                direction = SortDirection.Ascending;

            if (view.SortDescriptions.Count > 0)
            {
                view.SortDescriptions.Clear();

                var curColumn = GetSortedColumnHeader(listView);
                if (curColumn != null)
                {
                    var layer = AdornerLayer.GetAdornerLayer(curColumn);
                    var adorner = layer.GetAdorners(curColumn)?.FirstOrDefault(e => e is SortGlyphAdorner);

                    if (adorner != null)
                        layer.Remove(adorner);
                }
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                view.SortDescriptions.Add(new SortDescription(propertyName, (ListSortDirection)direction));
                SetSortedColumnHeader(listView, column);

                SetSortDirection(listView, direction);

                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(column);
                adornerLayer.Add(new SortGlyphAdorner(column, direction));
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
    }
}
