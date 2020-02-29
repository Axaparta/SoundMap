using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SoundMap.Waveforms
{
	[Serializable]
	public abstract class Waveform: Observable
	{
		private List<int> FIds = new List<int>();

		protected const double TwoPi = 2 * Math.PI;
		protected int FSampleHash = 0;


		public int Id { get; set; }

		[XmlIgnore]
		public int SampleRate { get; private set; }

		[XmlIgnore]
		public abstract string Name { get; }

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
			return Name.GetHashCode();
			//return FHashCode;
		}

		public override string ToString()
		{
			return Name ;
		}
	}
}
