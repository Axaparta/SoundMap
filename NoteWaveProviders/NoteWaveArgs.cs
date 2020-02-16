using SoundMap.Waveforms;
using System;
using System.Collections.Generic;

namespace SoundMap.NoteWaveProviders
{
	/// <summary>
	/// Дополнительный аргумент для вызова Read
	/// </summary>
	public class NoteWaveArgs : EventArgs
	{
		/// <summary>
		/// Коллекция всех актуальных сепмплов и их хэш-сумм
		/// </summary>
		public KeyValuePair<int, double[]>[] Samples { get; }
		public double MasterVolume { get; }
		public double MaxL { get; set; }
		public double MaxR { get; set; }

		public NoteWaveArgs(KeyValuePair<int, double[]>[] samples, double masterVolume)
		{
			Samples = samples;
			MasterVolume = masterVolume;
			MaxL = MaxR = 0;
		}

		public int SamplesHash
		{
			get
			{
				if (Samples.Length == 0)
					return 0;
				int r = 0;
				foreach (var s in Samples)
					r ^= s.Key;
				return r;
			}
		}
	}
}
