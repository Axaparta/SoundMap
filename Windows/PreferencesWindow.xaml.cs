using System.Windows;

namespace SoundMap.Windows
{
	/// <summary>
	/// Логика взаимодействия для PreferencesWindow.xaml
	/// </summary>
	public partial class PreferencesWindow : Window
	{
		public PreferencesWindow()
		{
			InitializeComponent();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
