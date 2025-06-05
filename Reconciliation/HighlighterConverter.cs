using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Reconciliation
{
    public class HighlighterConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is DataRowView)
            {
                var rowView = (DataRowView)values[1];
                var row = rowView.Row;

                if (row == null)
                    return Brushes.Transparent;

                foreach (var item in row.ItemArray)
                {
                    if (item.ToString().Contains(MismatchValueIdentifier.MicrosoftMarker))
                    {
                        return Brushes.Transparent;
                    }
                    if (item.ToString().Contains(MismatchValueIdentifier.SixDotOneMarker))
                    {
                        return Brushes.LightYellow;
                    }
                }
            }
            return SystemColors.AppWorkspace.IsKnownColor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
