using System.Windows;

namespace SoundMap
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			DataContext = new MainWindowModel();
			InitializeComponent();
		}

		private void Window_Closed(object sender, System.EventArgs e)
		{
			App.Settings.MainWindow.ReadFrom(this);
			((MainWindowModel)DataContext).WindowClose();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			App.Settings.MainWindow.ApplyTo(this);
		}
	}
}
