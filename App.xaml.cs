using System.Windows;

namespace SoundMap
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static void ShowError(string AMessage)
		{
			MessageBox.Show(AMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
		}
	}
}
