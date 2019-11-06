using System.Windows;
using System.Windows.Controls;

namespace SoundMap.Controls
{
	public partial class SoundPointControl : UserControl
	{
		public SoundPointControl()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty PointProperty = DependencyProperty.Register(
			"Point", typeof(SoundPoint), typeof(SoundPointControl), new FrameworkPropertyMetadata(null, Point_ChangedCallback));

		private static void Point_ChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as SoundPointControl).UpdateIsPointEnabled();
		}

		public SoundPoint Point
		{
			get => (SoundPoint)GetValue(PointProperty);
			set => SetValue(PointProperty, value);
		}

		public static readonly DependencyProperty IsPointEnabledProperty = DependencyProperty.Register(
			"IsPointEnabled", typeof(bool), typeof(SoundPointControl), new FrameworkPropertyMetadata(false));

		public bool IsPointEnabled
		{
			get => (bool)GetValue(IsPointEnabledProperty);
			set => SetValue(IsPointEnabledProperty, value);
		}

		internal void UpdateIsPointEnabled()
		{
			IsPointEnabled = (Point != null);
		}
	}
}
