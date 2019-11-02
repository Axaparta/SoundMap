using System.Windows;
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
			((MainWindowModel)DataContext).WindowClose();
		}
	}
}
