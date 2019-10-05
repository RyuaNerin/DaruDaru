using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Markup;

namespace DaruDaru.Core.Windows.Commands
{
    [ContentProperty("KeyGestures")]
    public class RoutedUICommand2 : RoutedUICommand
    {
        public RoutedUICommand2()
            : base()
        {
            this.KeyGestures = new ObservableCollection<KeyGesture2>();

            this.KeyGestures.CollectionChanged += this.KeyGestures_CollectionChanged;
        }

        public ObservableCollection<KeyGesture2> KeyGestures { get; }

        private void KeyGestures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.Update();

            if (e.OldItems != null)
                foreach (var item in e.OldItems.Cast<KeyGesture2>())
                    item.PropertyChanged -= this.Item_PropertyChanged;

            if (e.NewItems != null)
                foreach (var item in e.NewItems.Cast<KeyGesture2>())
                    item.PropertyChanged += this.Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Update();
        }

        private readonly object m_lock = new object();
        private void Update()
        {
            lock (this.m_lock)
            {
                this.InputGestures.Clear();

                foreach (var item in this.KeyGestures.Select(le => le.KeyGesture).Where(le => le != null))
                    this.InputGestures.Add(item);
            }
        }
    }

    public class KeyGesture2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "")
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private Key m_key;
        public Key Key
        {
            get => this.m_key;
            set
            {
                this.m_key = value;
                this.OnPropertyChanged();
            }
        }

        private ModifierKeys m_modifier;
        public ModifierKeys Modifier
        {
            get => this.m_modifier;
            set
            {
                this.m_modifier = value;
                this.OnPropertyChanged();
            }
        }

        private string m_displayString;
        public string DisplayString
        {
            get => this.m_displayString;
            set
            {
                this.m_displayString = value;
                this.OnPropertyChanged();
            }
        }

        public KeyGesture KeyGesture
        {
            get
            {
                if (this.DisplayString != null)
                    return new KeyGesture(this.Key, this.Modifier, this.DisplayString);

                if (this.Modifier != ModifierKeys.None)
                    return new KeyGesture(this.Key, this.Modifier);

                if (this.Key != Key.None)
                    return new KeyGesture(this.Key);

                return null;
            }
        }
    }
}
