using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				(sender as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}
	}
}
