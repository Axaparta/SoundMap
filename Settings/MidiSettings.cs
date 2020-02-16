using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundMap.Settings
{
	[Serializable]
	public class MidiSettings: ICloneable
	{
		private static string[] FMidiInputNames = null;

		public MidiSettings()
		{ 
		}

		public string MidiInputName { get; set; }

		public static string[] MidiInputNames
		{
			get
			{
				if (FMidiInputNames == null)
				{
					FMidiInputNames = new string[MidiIn.NumberOfDevices];
					for (int i = 0; i < MidiIn.NumberOfDevices; i++)
						FMidiInputNames[i] = MidiIn.DeviceInfo(i).ProductName;
				}
				return FMidiInputNames;
			}
		}

		public MidiIn CreateMidiIn()
		{
			for (int i = 0; i < MidiIn.NumberOfDevices; i++)
			{
				if (MidiIn.DeviceInfo(i).ProductName == MidiInputName)
					return new MidiIn(i);
			}
			return null;
		}

		public bool HasMidiInput
		{
			get
			{
				//using (var m = CreateMidiIn())
				//	return m != null;
				return true;
			}
		}

		public MidiSettings Clone()
		{
			return new MidiSettings
			{
				MidiInputName = this.MidiInputName
			};
		}

		public static void UpdateDevices()
		{
			FMidiInputNames = null;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
