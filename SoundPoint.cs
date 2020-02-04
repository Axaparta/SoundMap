
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
		private Waveform FLeftWaveform = null;
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
				return FWaveform;
			}
			set
			{
				var wf = value.Clone();
				var lwf = value.Clone();
				wf.LinkedWaveforms.Add(lwf);
				if (FWaveform != null)
					wf.Init(FWaveform.SampleRate);
				FWaveform = wf;
				FLeftWaveform = lwf;
				WaveformName = FWaveform.Name;
			}
		}

		public string WaveformName { get; set; }

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
			var p = new SoundPoint()
			{
				FFrequency = this.FFrequency,
				FIsMute = this.FIsMute,
				FIsSelected = this.FIsSelected,
				FIsSolo = this.FIsSolo,
				FWaveform = this.FWaveform.Clone(),
				FLeftWaveform = this.FLeftWaveform.Clone(),
				FLFrequencyDelta = this.FLFrequencyDelta,
				FVolume = this.FVolume
			};

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
				if (FFrequency != value)
				{
					FFrequency = value;
					NotifyPropertyChanged(nameof(Frequency));
				}
			}
		}

		public override string ToString()
		{
			return Frequency.ToString("F2");
		}

		public string AsString => ToString();

		public SoundPointValue GetValue(double ATime)
		{
			var rv = FWaveform.GetValue(ATime, FFrequency);
			double lv = rv;

			if (FLFrequencyDelta != 0)
				lv = FLeftWaveform.GetValue(ATime, FFrequency + FLFrequencyDelta);

			return new SoundPointValue(lv, rv);
		}
	}
}
