using System;
using System.Xml.Serialization;

namespace SoundMap.Settings
{
	[Serializable]
	public class SoundMapSettings
	{
		private PreferencesSettings FPreferences = new PreferencesSettings();

		public SoundMapSettings()
		{
		}

		public WindowSettings MainWindow { get; set; } = new WindowSettings();

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

		[XmlIgnore]
		public bool IsModify
		{
			get
			{
				return true;
			}
		}
	}
}
