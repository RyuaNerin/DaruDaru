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

        public Brush BrushProtected  { get; set; }
        public Brush BrushError      { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (MaruComicState)value;
            if (state == MaruComicState.Complete_1_Downloaded)
                return this.BrushCompete;

            if (state == MaruComicState.Error_1_Error)
                return this.BrushError;

            if (state == MaruComicState.Complete_2_Archived)
                return this.BrushDownloaded;

            if (state == MaruComicState.Error_2_Protected)
                return this.BrushProtected;

            return this.BrushNormal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
