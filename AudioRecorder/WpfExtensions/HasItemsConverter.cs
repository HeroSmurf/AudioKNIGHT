using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AudioKnight.WpfExtensions
{
	public class HasItemsConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
			{
				return false;
			}

			var countProp = value.GetType().GetProperty("Count");
			var lengthProp = value.GetType().GetProperty("Length");

			int count;
			if (countProp != null)
			{
				count = (int)countProp.GetValue(value);
			} else if (lengthProp != null)
			{
				count = (int)lengthProp.GetValue(value);
			} else
			{
				throw new ApplicationException(string.Format("Cannot determine if value of type '{0}' has items. " +
					"It may not have a 'Count' or 'Length' property.", value.GetType()));
			}

			return count > 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
