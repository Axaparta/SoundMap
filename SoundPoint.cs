
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SoundMap
{
	public delegate void SoundPointEvent(SoundPoint APoint);

	[Serializable]
	public class SoundPoint : Observable, ICloneable
	{
		private bool FIsSelected = false;

		private bool FIsSolo = false;
		private bool FIsMute = false;
		private Waveform FWaveform = null;
		private double FLFrequencyDelta = 0;

		private double FVolume = 0;
		private double FFrequency = 0;

		/// <summary>True, когда являвется частью ноты. Необходима для оптимизации</summary>
		[XmlIgnore]
		public bool IsNote { get; set; } = false;

		[XmlIgnore]
		public bool IsSelected
		{
			get => FIsSelected;
			set
			{
				if (FIsSelected != value)
				{
					FIsSelected = value;
					NotifyPropertyChanged(nameof(IsSelected));
				}
			}
		}

		public SoundPoint()
		{ }

		[XmlIgnore]
		public Waveform Waveform
		{
			get
			{
				if (FWaveform == null)
					FWaveform = Waveform.DefaultWaveform;
				return FWaveform;
			}
			set
			{
				if (FWaveform != null)
				{
					value.Frequency = FWaveform.Frequency;
					value.Init(FWaveform.SampleRate);
				}
				FWaveform = value;
			}
		}

		[XmlIgnore]
		public string WaveformName
		{
			get => Waveform.Name;
			set
			{
				Waveform = Waveform.BuildinWaveForms.First(wf => wf.Name == value).Clone();
			}
		}

		public string WaveformData
		{
			get => Waveform.SerializeToString();
			set
			{
				FWaveform = Waveform.CreateFromString(value);
				if (FWaveform != null)
					FWaveform.Frequency = Frequency;
			}
		}

		public bool IsSolo
		{
			get => FIsSolo;
			set
			{
				if (FIsSolo != value)
				{
					FIsSolo = value;
					NotifyPropertyChanged(nameof(IsSolo));
				}
			}
		}

		public bool IsMute
		{
			get => FIsMute;
			set
			{
				if (FIsMute != value)
				{
					FIsMute = value;
					NotifyPropertyChanged(nameof(IsMute));
				}
			}
		}

		/// <summary>Необходима для группового перемещения</summary>
		[XmlIgnore]
		public SoundPoint Start { get; set; }

		object ICloneable.Clone()
		{
			return Clone();
		}

		public SoundPoint Clone()
		{
			var p = (SoundPoint)MemberwiseClone();
			p.Waveform = this.Waveform.Clone();
			return p;
		}

		public double Volume
		{
			get => FVolume;
			set
			{
				if (FVolume != value)
				{
					FVolume = value;
					NotifyPropertyChanged(nameof(Volume));
				}
			}
		}

		public double LeftFrequencyDelta
		{
			get => FLFrequencyDelta;
			set
			{
				if (FLFrequencyDelta != value)
				{
					FLFrequencyDelta = value;
					NotifyPropertyChanged(nameof(LeftFrequencyDelta));
				}
			}
		}

		public double Frequency
		{
			get => FFrequency;
			set
			{
				//if (FFrequency != value)
				{
					FFrequency = value;
					Waveform.Frequency = Frequency;
					NotifyPropertyChanged(nameof(Frequency));
				}
			}
		}

		public override string ToString()
		{
			return Frequency.ToString("F2");
		}

		public string AsString => ToString();

		//private double GetValue(double AFrequency, double ATime)
		//{
		//	return Math.Sin(AFrequency * ATime * Math.PI * 2);
		//}

		public SoundPointValue GetValue(double ATime)
		{
			var rv = Waveform.GetValue(ATime);
			double lv = rv;

			//if (FLFrequencyDelta != 0)
			//	lv = GetValue(FFrequency + FLFrequencyDelta, ATime);

			return new SoundPointValue(lv, rv);
		}
	}
}
