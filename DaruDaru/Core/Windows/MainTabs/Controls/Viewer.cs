using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DaruDaru.Config;
using DaruDaru.Marumaru;
using DaruDaru.Utilities;

namespace DaruDaru.Core.Windows.MainTabs.Controls
{
    internal class Viewer : Control
    {
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

        static Viewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Viewer), new FrameworkPropertyMetadata(typeof(Viewer)));
        }

        public Viewer()
        {
            this.m_filterClearCommand = new SimpleCommand(e =>
            {
                this.Text = null;

                if (this.UseSearch)
                {
                    this.m_filterMode = FileterModes.NoFilter;
                    this.ICollectionView?.Refresh();
                }
            });

            this.OnApplyTemplate();
        }

        public ICommand TextBoxClearCommand => this.m_filterClearCommand;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.m_textBox     = GetTemplateChild(TextBoxName ) as TextBox;
            this.m_button      = GetTemplateChild(ButtonName  ) as Button;
            this.m_listView    = GetTemplateChild(ListName    ) as ListView;
            this.m_gridView    = GetTemplateChild(ListViewName) as GridView;

            if (this.m_button != null)
            {
                this.m_button.Click -= this.Button_Click;
                this.m_button.Click += this.Button_Click;
            }

            if (this.m_textBox != null)
            {
                this.m_textBox.KeyDown -= this.TextBox_KeyDown;
                this.m_textBox.KeyDown += this.TextBox_KeyDown;
            }
        }

        private TextBox m_textBox;
        private Button m_button;
        private ListView m_listView;
        private GridView m_gridView;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Viewer), new PropertyMetadata(null));
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextWatermarkProperty = DependencyProperty.Register("TextWatermark", typeof(string), typeof(Viewer), new PropertyMetadata(null));
        public string TextWatermark
        {
            get => (string)this.GetValue(TextWatermarkProperty);
            set => this.SetValue(TextWatermarkProperty, value);
        }

        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register("ButtonContent", typeof(object), typeof(Viewer), new PropertyMetadata(null));
        public object ButtonContent
        {
            get => this.GetValue(ButtonContentProperty);
            set => this.SetValue(ButtonContentProperty, value);
        }

        public static readonly DependencyProperty ListItemSourceProperty = DependencyProperty.Register("ListItemSource", typeof(IList), typeof(Viewer), new PropertyMetadata(null, new PropertyChangedCallback((d, e) => ((Viewer)d).m_collectionView = null)));
        public IList ListItemSource
        {
            get => (IList)this.GetValue(ListItemSourceProperty);
            set => this.SetValue(ListItemSourceProperty, value);
        }

        public static readonly DependencyProperty ListContextMenuProperty = DependencyProperty.Register("ListContextMenu", typeof(ContextMenu), typeof(Viewer), new PropertyMetadata(null));
        public ContextMenu ListContextMenu
        {
            get => (ContextMenu)this.GetValue(ListContextMenuProperty);
            set => this.SetValue(ListContextMenuProperty, value);
        }

        public static readonly DependencyProperty ListViewProperty = DependencyProperty.Register("ListView", typeof(ViewBase), typeof(Viewer), new PropertyMetadata(null));
        public ViewBase ListView
        {
            get => (ViewBase)this.GetValue(ListViewProperty);
            set => this.SetValue(ListViewProperty, value);
        }

        public DaruUriParser DaruUriParser { get; set; }

        private bool m_useSearch = true;
        public bool UseSearch
        {
            get => this.m_useSearch;
            set
            {
                this.m_useSearch = value;
                this.m_collectionView = null;
            }
        }

        public event RoutedEventHandler ButtonClick;

        public object SelectedItem => this.m_listView?.SelectedItem;
        public IList SelectedItems => this.m_listView?.SelectedItems;
        
        private readonly ICommand m_filterClearCommand;

        private FileterModes m_filterMode = 0;
        private string[] m_FilterCodes;
        private string m_filterString;

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
        }

        private bool Filter(object item)
        {
            if (this.m_filterMode == FileterModes.NoFilter)
                return true;

            var entry = (IEntry)item;

            switch (this.m_filterMode)
            {
                case FileterModes.ByCode: return entry.Code == this.m_filterString;
                case FileterModes.String: return entry.Text.Contains(this.m_filterString);
                case FileterModes.ByCodes: return Array.IndexOf(this.m_FilterCodes, entry.Code) >= 0;
            }

            return true;
        }

        public void FilterByCode(string[] codes, string text)
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

        public void FocusTextBox()
        {
            this.m_textBox?.SelectAll();
            this.m_textBox?.Focus();
        }
    }
}
