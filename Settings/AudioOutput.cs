using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace SoundMap.Settings
{
	public class AudioOutput : Observable
	{
		private static readonly string[] WasapiChannels = new string[] { "Stereo" };
		private static readonly int[] AsioSampleRates = new int[] { 44100, 48000, 96000, 192000 };

		private readonly MMDevice FMmDevice;
		private readonly string FAsioName;

		public string Name { get; }

		public string[] Channels { get; }
		public int[] SampleRates { get; }
		public int DefaultLatency { get; }

		public bool LatencyEnbled
		{
			get => FMmDevice != null;
		}

		public AudioOutput(MMDevice AMmDevice)
		{
			FAsioName = null;
			FMmDevice = AMmDevice;

			SampleRates = new int[] { FMmDevice.AudioClient.MixFormat.SampleRate };
			Channels = WasapiChannels;

			DefaultLatency = 250;
			Name = "WASAPI: " + AMmDevice.FriendlyName;
		}

		public AudioOutput(string AAsioName)
		{
			FMmDevice = null;
			SampleRates = AsioSampleRates;

			FAsioName = AAsioName;

			using (var ad = new AsioOut(FAsioName))
			{
				List<string> chs = new List<string>(ad.DriverOutputChannelCount / 2);
				for (int i = 0; i < ad.DriverOutputChannelCount; i += 2)
					chs.Add($"{ad.AsioOutputChannelName(i)}/{ad.AsioOutputChannelName(i + 1)}");
				Channels = chs.ToArray();
				DefaultLatency = ad.PlaybackLatency;
			}

			Name = "ASIO: " + AAsioName;
		}

		public IWavePlayer CreateOutput(PreferencesSettings ASettings)
		{
			if (FMmDevice != null)
				return new WasapiOut(FMmDevice, AudioClientShareMode.Shared, false, ASettings.Latency);

			if (FAsioName != null)
			{
				var ao = new AsioOut(FAsioName);
				int index = -1;
				for (int i = 0; i < Channels.Length; i++)
					if (Channels[i] == ASettings.Channel)
					{
						index = i;
						break;
					}
				if (index != -1)
					ao.ChannelOffset = index * 2;

				return ao;
			}
				

			throw new NotImplementedException();
		}
	}
}
