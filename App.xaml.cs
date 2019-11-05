using SoundMap.Settings;
using System;
using System.IO;
using System.Windows;

namespace SoundMap
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string AppName { get; } = "SoundMap";
		public static readonly string ConfigurePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
		public static readonly string SettingsFileName = Path.Combine(ConfigurePath, "Settings.xml");
		public static SoundMapSettings Settings { get; private set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			try
			{
				if (File.Exists(SettingsFileName))
				{
					Settings = XmlHelper.Load<SoundMapSettings>(SettingsFileName);
				}
				else
					Settings = new SoundMapSettings();
			}
			catch (Exception ex)
			{
				ShowError("Load settings error: " + ex.Message);
				Settings = new SoundMapSettings();
			}
			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (Settings.IsModify)
				try
				{
					XmlHelper.Save(Settings, SettingsFileName);
				}
				catch (Exception ex)
				{
					ShowError("Save settings error: " + ex.Message);
				}
			base.OnExit(e);
		}

		public static void ShowError(string AMessage)
		{
			MessageBox.Show(AMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
		}
	}
}
