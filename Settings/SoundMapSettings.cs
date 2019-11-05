using System;
using System.Xml.Serialization;

namespace SoundMap.Settings
{
	[Serializable]
	public class SoundMapSettings
	{
		public SoundMapSettings()
		{
		}

		public string DeviceId { get; set; }
		public WindowSettings MainWindow { get; set; } = new WindowSettings();


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
