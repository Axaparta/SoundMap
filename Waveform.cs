using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SoundMap
{
	public abstract class Waveform
	{
		private static Waveform[] FBuildinWaveForms = null;
		protected const double TwoPi = 2 * Math.PI;
		

		public abstract string Name { get; }

		public virtual void Init(int ASampleRate)
		{
		}

		public abstract double GetValue(double AFrequency, double ATime);

		public static Waveform[] BuildinWaveForms
		{
			get
			{
				if (FBuildinWaveForms == null)
				{
					FBuildinWaveForms = typeof(Waveform).Assembly.GetTypes().
						Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Waveform))).
						Select(tt => (Waveform)Activator.CreateInstance(tt)).ToArray();
				}
				return FBuildinWaveForms;
			}
		}

		public static Waveform DefaultWaveform => BuildinWaveForms.First(wf => wf is SineWaveform);

		public static Waveform CreateFromString(string AData)
		{
			return BuildinWaveForms.FirstOrDefault(wf => wf.Name == AData);
		}

		public string SerializeToString()
		{
			return Name;
		}
	}

	public class SineWaveform: Waveform
	{
		public override string Name => "Sine";

		public override double GetValue(double AFrequency, double ATime)
		{
			return Math.Sin(TwoPi * ATime * AFrequency);
		}
	}
}
