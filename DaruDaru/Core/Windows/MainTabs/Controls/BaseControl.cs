using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DaruDaru.Config;
using DaruDaru.Marumaru;
using DaruDaru.Utilities;
using MahApps.Metro.Controls;

namespace DaruDaru.Core.Windows.MainTabs.Controls
{
    internal class DragDropStartedEventArgs : EventArgs
    {
        public DragDropStartedEventArgs(IList iList)
        {
            this.IList = iList;
        }

        public IList IList { get; private set; }
        public string DataFormat { get; set; }
        public object Data { get; set; }
        public DragDropEffects AllowedEffects { get; set; }
    }

    internal delegate void DragDropStartedEventHandler(object sender, DragDropStartedEventArgs e);

    internal class BaseControl : Control
    {
        protected static RoutedUICommand Create(string text, string name, Type type)
            => new RoutedUICommand(text, name, typeof(Archive));

        protected static RoutedUICommand Create(string text, string name, Type type, params (Key key, ModifierKeys mkey)[] commands)
        {
            var command = Create(text, name, type);

            foreach (var (key, mkey) in commands)
                command.InputGestures.Add(new KeyGesture(key, mkey));

            return command;
        }

        private const string TextBoxName  = "PART_TextBox";
        private const string ButtonName   = "PART_Button";
        private const string ListName     = "PART_ListView";
        private const string ListMenuName = "PART_ContextMenu";
        private const string ListViewName = "PART_GridView";

        private enum FileterModes
        {
            NoFilter,
            ByCode,
            ByCodes,
            String
        }

        private class SimpleCommand : ICommand
        {
            public SimpleCommand(Action<object> execute)
            {
                this.m_execute = execute;
            }

            private readonly Action<object> m_execute;

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object parameter)
                => true;

            public void Execute(object parameter)
                => this.m_execute(parameter);
        }

        static BaseControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BaseControl), new FrameworkPropertyMetadata(typeof(BaseControl)));
        }

        public BaseControl()
        {
            this.m_textBoxCommand = new SimpleCommand(e =>
            {
                this.Text = null;

                if (this.UseSearch)
                {
                    this.m_filterMode = FileterModes.NoFilter;
                    this.ICollectionView?.Refresh();
                }
            });

            /*
            <Style TargetType="{x:Type ListViewItem}"
                   BasedOn="{StaticResource MetroListViewItem}">
                <EventSetter Event="MouseDoubleClick" Handler="ListViewItem_ListViewItemDoubleClick" />
            </Style>
            */
            this.m_listViewItemStyle = new Style(typeof(ListViewItem), this.FindResource("MetroListViewItem") as Style);
            this.m_listViewItemStyle.Setters.Add(new EventSetter(ListViewItem.MouseDoubleClickEvent, new MouseButtonEventHandler(this.ListViewItem_ListViewItemDoubleClick)));

            this.OnApplyTemplate();
        }

        private readonly Style m_listViewItemStyle;
        private readonly ICommand m_textBoxCommand;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            this.TextBoxControl  = GetTemplateChild(TextBoxName ) as TextBox;
            this.ButtonControl   = GetTemplateChild(ButtonName  ) as Button;
            this.ListViewControl = GetTemplateChild(ListName    ) as ListView;

            this.Resources[typeof(ListViewItem)] = this.m_listViewItemStyle;
        }

        private TextBox m_textBox;
        private TextBox TextBoxControl
        {
            get => this.m_textBox;
            set
            {
                if (this.m_textBox == value)
                    return;

                if (this.m_textBox != null)
                {
                    TextBoxHelper.SetButtonCommand(this.m_textBox, TextBoxHelper.ButtonCommandProperty.DefaultMetadata.DefaultValue as ICommand);

                    this.m_textBox.KeyDown -= this.TextBox_KeyDown;
                }

                this.m_textBox = value;

                if (this.m_textBox != null)
                {
                    TextBoxHelper.SetButtonCommand(this.m_textBox, this.m_textBoxCommand);

                    this.m_textBox.KeyDown += this.TextBox_KeyDown;
                }
            }
        }

        private Button m_button;
        private Button ButtonControl
        {
            get => this.m_button;
            set
            {
                if (this.m_button == value)
                    return;

                if (this.m_button != null)
                    this.m_button.Click -= this.Button_Click;

                this.m_button = value;

                if (this.m_button != null)
                    this.m_button.Click += this.Button_Click;
            }
        }

        private ListView m_listView;
        private ListView ListViewControl
        {
            get => this.m_listView;
            set
            {
                if (this.m_listView == value)
                    return;

                if (this.m_listView != null)
                {
                    this.m_listView.PreviewMouseDown -= this.ListView_PreviewMouseDown;
                    this.m_listView.MouseMove        -= this.ListView_MouseMove;
                    this.m_listView.PreviewMouseUp   -= this.ListView_PreviewMouseUp;
                }

                this.m_listView = value;
                this.m_collectionView = null;

                if (this.m_listView != null)
                {
                    this.m_listView.PreviewMouseDown += this.ListView_PreviewMouseDown;
                    this.m_listView.MouseMove        += this.ListView_MouseMove;
                    this.m_listView.PreviewMouseUp   += this.ListView_PreviewMouseUp;
                }
            }
        }

        protected internal static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(BaseControl), new PropertyMetadata(null));
        protected internal string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        protected internal static readonly DependencyProperty TextWatermarkProperty = DependencyProperty.Register("TextWatermark", typeof(string), typeof(BaseControl), new PropertyMetadata(null));
        protected internal string TextWatermark
        {
            get => (string)this.GetValue(TextWatermarkProperty);
            set => this.SetValue(TextWatermarkProperty, value);
        }

        protected internal static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register("ButtonContent", typeof(object), typeof(BaseControl), new PropertyMetadata(null));
        protected internal object ButtonContent
        {
            get => this.GetValue(ButtonContentProperty);
            set => this.SetValue(ButtonContentProperty, value);
        }

        protected internal static readonly DependencyProperty ListItemSourceProperty = DependencyProperty.Register("ListItemSource", typeof(IList), typeof(BaseControl), new PropertyMetadata(null, new PropertyChangedCallback((d, e) => ((BaseControl)d).m_collectionView = null)));
        protected internal IList ListItemSource
        {
            get => (IList)this.GetValue(ListItemSourceProperty);
            set => this.SetValue(ListItemSourceProperty, value);
        }

        protected internal static readonly DependencyProperty ListContextMenuProperty = DependencyProperty.Register("ListContextMenu", typeof(ContextMenu), typeof(BaseControl), new PropertyMetadata(null));
        protected internal ContextMenu ListContextMenu
        {
            get => (ContextMenu)this.GetValue(ListContextMenuProperty);
            set => this.SetValue(ListContextMenuProperty, value);
        }

        protected internal static readonly DependencyProperty ListViewProperty = DependencyProperty.Register("ListView", typeof(ViewBase), typeof(BaseControl), new PropertyMetadata(null));
        protected internal ViewBase ListView
        {
            get => (ViewBase)this.GetValue(ListViewProperty);
            set => this.SetValue(ListViewProperty, value);
        }

        public DaruUriParser DaruUriParser { get; set; }

        public event DragDropStartedEventHandler DragDropStarted;

        public event MouseButtonEventHandler ListViewItemDoubleClick;

        private bool m_useSearch = true;
        protected internal bool UseSearch
        {
            get => this.m_useSearch;
            set
            {
                this.m_useSearch = value;
                this.m_collectionView = null;
            }
        }

        protected internal event RoutedEventHandler ButtonClick;

        protected internal object SelectedItem => this.m_listView?.SelectedItem;
        protected internal IList SelectedItems => this.m_listView?.SelectedItems;

        protected internal T[] Get<T>()
            => this.m_listView?.SelectedItems.OfType<T>().ToArray() ?? new T[0];

        private ICollectionView m_collectionView;
        private ICollectionView ICollectionView
        {
            get
            {
                if (this.m_collectionView == null)
                {
                    this.m_collectionView = CollectionViewSource.GetDefaultView(this.ListItemSource);                    
                    this.m_collectionView.Filter = this.Filter;
                }

                return this.m_collectionView;
            }
            set => this.m_collectionView = value;
        }
        
        private FileterModes m_filterMode = 0;
        private string[] m_FilterCodes;
        private string m_filterString;
        private bool Filter(object item)
        {
            if (this.m_filterMode == FileterModes.NoFilter)
                return true;

            var entry = (IEntry)item;

            switch (this.m_filterMode)
            {
                case FileterModes.ByCode:  return entry.Code == this.m_filterString;
                case FileterModes.String:  return entry.Text.Contains(this.m_filterString);
                case FileterModes.ByCodes: return Array.IndexOf(this.m_FilterCodes, entry.Code) >= 0;
            }

            return true;
        }

        public void SearchArchiveByCodes(string[] codes, string text)
        {
            this.Text = text;

            this.m_filterMode = FileterModes.ByCodes;
            this.m_FilterCodes = codes;
            this.ICollectionView?.Refresh();
        }
        
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.Search(((TextBox)sender).Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Search(this.Text);
        }

        private void Search(string str)
        {
            if (!this.UseSearch)
                this.ButtonClick?.Invoke(this, null);
            else
            {
                if (string.IsNullOrWhiteSpace(str))
                    this.m_filterMode = FileterModes.NoFilter;
                else
                {
                    var code = Utility.TryCreateUri(str, out Uri uri) ? this.DaruUriParser?.GetCode(uri) : null;

                    if (code == null)
                    {
                        this.m_filterMode = FileterModes.String;
                        this.m_filterString = str;
                    }
                    else
                    {
                        this.m_filterMode = FileterModes.ByCode;
                        this.m_filterString = code;
                    }
                }
            }

            this.ICollectionView.Refresh();
        }

        protected virtual string GetCode(Uri uri) => null;

        private Point m_listViewPoint;
        private bool m_listViewLeftButton;
        private void ListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.m_listViewPoint = e.GetPosition(null);
            this.m_listViewLeftButton = true;
        }

        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.m_listViewLeftButton || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (this.DragDropStarted == null) return;

            var diff = this.m_listViewPoint - e.GetPosition(null);

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var listView = sender as ListView;

                if (!CheckAncestor<ListViewItem>((DependencyObject)e.OriginalSource))
                    return;
                
                var arg = new DragDropStartedEventArgs(new ArrayList(listView.SelectedItems));
                this.DragDropStarted.Invoke(sender, arg);

                if (arg.AllowedEffects != DragDropEffects.None && arg.Data != null)
                {
                    var dataObject = new DataObject(arg.DataFormat, arg.Data);

                    DragDrop.DoDragDrop(listView, dataObject, arg.AllowedEffects);
                }
            }
        }
        private static bool CheckAncestor<T>(DependencyObject tem)
            where T : DependencyObject
        {
            do
            {
                if (tem is T)
                    return true;

                tem = VisualTreeHelper.GetParent(tem);
            }
            while (tem != null);

            return false;
        }

        private void ListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.m_listViewLeftButton = false;
        }

        public void FocusTextBox()
        {
            this.TextBoxControl?.SelectAll();
            this.TextBoxControl?.Focus();
        }

        private void ListViewItem_ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ListViewItemDoubleClick?.Invoke(sender, e);
        }
    }
}
