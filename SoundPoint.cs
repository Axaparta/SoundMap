
using SoundMap.Waveforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private bool FIsMute = false;
		private Waveform FWaveform = null;
		private double FVolume = 0;
		private double FFrequency = 0;
		private string FWaveformName = string.Empty;

		[XmlIgnore]
		public SoundProject Project { get; set; }

		[XmlIgnore]
		public double LeftPct { get; private set; } = 1;
		[XmlIgnore]
		public double RightPct { get; private set; } = 1;

		public SoundPoint()
		{ }

		public SoundPoint(SoundProject project, double frequency, double volume)
		{
			Project = project;
			FFrequency = frequency;
			FVolume = volume;
		}

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

		[XmlIgnore]
		public Waveform Waveform
		{
			get
			{
				Debug.Assert(Project != null);
				if (FWaveform == null)
					FWaveform = Project.GetWaveform(WaveformName);
				return FWaveform;
			}
		}

		public string WaveformName
		{
			get
			{
				return FWaveformName;
			}
			set
			{
				if (FWaveformName != value)
				{
					FWaveformName = value;
					FWaveform = null;
					NotifyPropertyChanged(nameof(WaveformName));
					NotifyPropertyChanged(nameof(Waveform));
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
				FIsSelected = this.FIsSelected,
				FIsMute = this.FIsMute,
				FFrequency = this.FFrequency,
				FVolume = this.FVolume,
				FWaveformName = this.FWaveformName,
				FWaveform = this.FWaveform
			};
			p.Project = this.Project;
			p.LeftPct = this.LeftPct;
			p.RightPct = this.RightPct;
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

		/// <summary>
		/// -1 = L 100, R 0
		///  0 = L 100, R 100
		/// +1 = L 0  , R 100
		/// </summary>
		public double LRBalance
		{
			get
			{
				if (RightPct < 1)
					return RightPct - 1;
				if (LeftPct < 1)
					return 1 - LeftPct;
				return 0;
			}
			set
			{
				if (value < -1)
					value = -1;
				if (value > 1)
					value = 1;

				LeftPct = 1;
				RightPct = 1;

				if (value < 0)
					RightPct = 1 + value;
				if (value > 0)
					LeftPct = 1 - value;
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

		//public string AsString => ToString();

		//public SoundPointValue GetValue(double ATime)
		//{
		//	var rv = FWaveform.GetValue(ATime, FFrequency);
		//	rv = rv * Volume;
		//	return new SoundPointValue(rv*LeftPct, rv*RightPct);
		//}
	}
}
