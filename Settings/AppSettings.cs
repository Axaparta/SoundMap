using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoundMap.Settings
{
	[Serializable]
	public class AppSettings: Observable
	{
		private PreferencesSettings FPreferences = new PreferencesSettings();
		private readonly List<string> FFileHistory = new List<string>();

		public AppSettings()
		{
		}

		public WindowSettings MainWindow { get; set; } = new WindowSettings();

		public string[] FileHistory
		{
			get => FFileHistory.ToArray();
			set
			{
				if (value != null)
				{
					FFileHistory.Clear();
					FFileHistory.AddRange(value);
				}
			}
		}

		public void AddHistory(string AFileName)
		{
			var p = FFileHistory.IndexOf(AFileName);
			if (p != -1)
				FFileHistory.RemoveAt(p);
			FFileHistory.Insert(0, AFileName);

			while (FFileHistory.Count > 8)
				FFileHistory.RemoveAt(FFileHistory.Count - 1);

			NotifyPropertyChanged(nameof(FileHistory));
			NotifyPropertyChanged(nameof(HasFileHistory));
		}

		public bool HasFileHistory
		{
			get => FFileHistory.Count > 0;
		}

		public PreferencesSettings Preferences
		{
			get
			{
				if (FPreferences == null)
					FPreferences = new PreferencesSettings();
				return FPreferences;
			}
			set => FPreferences = value;
		}
	}
}
