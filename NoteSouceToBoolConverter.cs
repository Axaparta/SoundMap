using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace SoundMap
{
	public class NoteSouceToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is NoteSourceEnum ns)
				if (parameter is NoteSourceEnum par)
					return ns == par;
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool bv)
				if (bv && (parameter is NoteSourceEnum param))
					return param;
			return DependencyProperty.UnsetValue;
		}
	}
}
