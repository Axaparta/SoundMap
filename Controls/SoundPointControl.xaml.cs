using SoundMap.Waveforms;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

		public static readonly DependencyProperty WaveformsProperty = DependencyProperty.Register(
			"Waveforms", typeof(ObservableCollection<Waveform>), typeof(SoundPointControl), new FrameworkPropertyMetadata(new ObservableCollection<Waveform>()));

		public ObservableCollection<Waveform> Waveforms
		{
			get => (ObservableCollection<Waveform>)GetValue(WaveformsProperty);
			set => SetValue(WaveformsProperty, value);
		}

		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				(sender as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}
	}
}
