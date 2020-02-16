using System;
using System.Xml.Serialization;

namespace SoundMap.Waveforms
{
	[Serializable]
	public abstract class Waveform: Observable
	{
		protected const double TwoPi = 2 * Math.PI;

		[XmlIgnore]
		public int SampleRate { get; private set; }

		[XmlIgnore]
		public abstract string Name { get; }

		//[XmlIgnore]
		//public static Dictionary<WaveBufferKey, double[]> Samples { get; } = new Dictionary<WaveBufferKey, double[]>();

		public virtual void Init(int ASampleRate)
		{
			SampleRate = ASampleRate;
		}

		public abstract double GetValue(double ATime, double AFrequency);

		//public virtual Waveform Clone()
		//{
		//	var r = (Waveform)MemberwiseClone();
		//	r.LinkedWaveforms = new List<Waveform>();
		//	r.SampleRate = this.SampleRate;
		//	return r;
		//}

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return obj.GetHashCode() == this.GetHashCode();
		}

		public override int GetHashCode()
		{
			return (Name == null) ? 0 : Name.GetHashCode();
		}
	}
}
