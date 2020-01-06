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
		private MMDevice MmDevice { get; }
		private string AsioName { get; }
		public string Name { get; }

		public int? SampleRate { get; }
		public int Channels { get; }
		public int Latency { get; }

		public AudioOutput(MMDevice AMmDevice)
		{
			AsioName = null;

			MmDevice = AMmDevice;
			SampleRate = MmDevice.AudioClient.MixFormat.SampleRate;
			Channels = MmDevice.AudioClient.MixFormat.Channels;
			//Latency = (int)MmDevice.AudioClient.StreamLatency;
			Latency = 25;
			Name = "WASAPI: " + AMmDevice.FriendlyName;
		}

		public AudioOutput(string AAsioName)
		{
			MmDevice = null;
			SampleRate = null;

			AsioName = AAsioName;

			using (var ad = new AsioOut(AsioName))
			{
				Channels = ad.DriverOutputChannelCount;
				Latency = ad.PlaybackLatency;
			}

			Name = "ASIO: " + AAsioName;
		}

		public IWavePlayer CreateOutput(PreferencesSettings ASettings)
		{
			if (MmDevice != null)
				return new WasapiOut(MmDevice, AudioClientShareMode.Shared, false, ASettings.Latency);

			if (AsioName != null)
				return new AsioOut(AsioName);

			throw new NotImplementedException();
		}

		public bool AllowSampleRateEdit => !SampleRate.HasValue;
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
							aos.Add(new AudioOutput(n));

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

		public static int[] SampleRates { get; } = new int[] { 44100, 48000, 96000, 192000 };

		public PreferencesSettings()
		{ }

		public string SelectedAudioOutputName { get; set; }
		private int? FLatency = null;
		private int? FSampleRate = null;

		[XmlIgnore]
		public AudioOutput SelectedAudioOutput
		{
			get
			{
				var r = AudioOutputs.FirstOrDefault(ao => ao.Name == SelectedAudioOutputName);
				if (r == null)
					return DefaultAudioOutput;
				return r;
			}
			set
			{
				if (value == null)
					SelectedAudioOutputName = null;
				else
					SelectedAudioOutputName = value.Name;
			}
		}

		public IWavePlayer CreateOutput() => SelectedAudioOutput.CreateOutput(this);

		public int Latency
		{
			get
			{
				if (FLatency.HasValue)
					return FLatency.Value;
				return SelectedAudioOutput.Latency;
			}
			set
			{
				if (value == 0)
					FLatency = null;
				else
					FLatency = value;
			}
		}

		public int SampleRate
		{
			get
			{
				var sao = SelectedAudioOutput;
				if (sao.SampleRate.HasValue)
					return sao.SampleRate.Value;
				if (FSampleRate.HasValue)
					return FSampleRate.Value;
				return SampleRates.First();
			}
			set => FSampleRate = value;
		}

		public int Channels => SelectedAudioOutput.Channels;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public PreferencesSettings Clone()
		{
			var r = new PreferencesSettings();
			r.SelectedAudioOutputName = this.SelectedAudioOutputName;
			return r;
		}
	}
}
