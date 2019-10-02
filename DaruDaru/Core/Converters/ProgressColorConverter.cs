using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DaruDaru.Marumaru.ComicInfo;

namespace DaruDaru.Core.Converters
{
    internal class ProgressColorConverter : IValueConverter
    {
        public Brush BrushNormal     { get; set; }

        public Brush BrushCompete    { get; set; }
        public Brush BrushDownloaded { get; set; }
        public Brush BrushNoNew      { get; set; }
        public Brush BrushSkip       { get; set; }

        public Brush BrushError      { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (MaruComicState)value;

            switch (state)
            {
                case MaruComicState.Complete_1_Downloaded:  return this.BrushCompete;
                case MaruComicState.Complete_2_Archived:    return this.BrushDownloaded;
                case MaruComicState.Complete_3_NoNew:       return this.BrushNoNew;
                case MaruComicState.Complete_4_Skip:        return this.BrushSkip;
            }

            if (state.HasFlag(MaruComicState.Error))
                return this.BrushError;

            return this.BrushNormal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
