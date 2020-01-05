using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SoundMap.Settings
{
	public class AudioOutput
	{
		private AsioOut FAsioOut;
		private WasapiOut FWasapiOut;

		public AudioOutput(MMDevice AMmDevice)
		{
			FWasapiOut = new WasapiOut(AMmDevice, AudioClientShareMode.Exclusive, true, 25);
			Name = "WASAPI: " + AMmDevice.FriendlyName;
		}

		public AudioOutput(AsioOut AAsioOut)
		{
			FAsioOut = AAsioOut;
			Name = "ASIO: " + AAsioOut.DriverName;
		}

		public string Name { get; set; }
	}

	[Serializable]
	public class PreferencesSettings: ICloneable
	{
		private static AudioOutput[] FAudioOutputs = null;
		private static AudioOutput FDefaultAudioOutput = null;

		public static AudioOutput[] AudioOutputs
		{
			get
			{
				if (FAudioOutputs == null)
				{
					List<AudioOutput> aos = new List<AudioOutput>();

					using (var e = new MMDeviceEnumerator())
					{
						//FDefaultDeviceId = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
						foreach (MMDevice d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
							aos.Add(new AudioOutput(d));
					}

					if (AsioOut.isSupported())
						foreach (var n in AsioOut.GetDriverNames())
							aos.Add(new AudioOutput(new AsioOut(n)));

					FAudioOutputs = aos.ToArray();
				}

				return FAudioOutputs;
			}
		}

		public static AudioOutput DefaultAudioOutput
		{
			get
			{
				if (FDefaultAudioOutput == null)
				{
					using (var e = new MMDeviceEnumerator())
					{
						FDefaultAudioOutput = new AudioOutput(e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
					}
				}
				return FDefaultAudioOutput;
			}
		}

		public PreferencesSettings()
		{ }

		public string AudioOutputName { get; set; }

		[XmlIgnore]
		public AudioOutput AudioOutput
		{
			get
			{
				var r = AudioOutputs.FirstOrDefault(ao => ao.Name == AudioOutputName);
				if (r == null)
					return DefaultAudioOutput;
				return r;
			}
			set
			{
				if (value == null)
					AudioOutputName = string.Empty;
				else
					AudioOutputName = value.Name;
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public PreferencesSettings Clone()
		{
			var r = new PreferencesSettings();
			r.AudioOutputName = this.AudioOutputName;
			return r;
		}
	}
}
