using System;

namespace SoundMap.Waveforms
{
	public class SineWaveform : Waveform
	{
		public override string Name => "Sine";

		public override double GetValue(double ATime, double AFrequency)
		{
			return Math.Sin(TwoPi * ATime * AFrequency);
		}
	}
}
