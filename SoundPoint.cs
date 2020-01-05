
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
		private PointKind FKind = PointKind.Static;
		private double FLFrequencyDelta = 0;

		private double FVolume = 0;
		private double FRFrequency = 0;

		//public event Action<SoundPoint> RemoveSelf;

		//public static PointKind[] PointKinds { get; } = Enum.GetValues(typeof(PointKind)).Cast<PointKind>().ToArray();
		public static KeyValuePair<PointKind, string>[] PointKinds { get; } = Enum.GetValues(typeof(PointKind)).Cast<PointKind>().Select(p => new KeyValuePair<PointKind, string>(p, p.ToString())).ToArray();

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

		public PointKind Kind
		{
			get => FKind;
			set
			{
				if (FKind != value)
				{
					FKind = value;
					NotifyPropertyChanged(nameof(Kind));
				}
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
			return (SoundPoint)MemberwiseClone();
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
			get => FRFrequency;
			set
			{
				if (FRFrequency != value)
				{
					FRFrequency = value;
					NotifyPropertyChanged(nameof(Frequency));
				}
			}
		}

		public override string ToString()
		{
			return Frequency.ToString("F2");
		}

		private double GetValue(double AFrequency, double ATime)
		{
			return Math.Sin(AFrequency * ATime * Math.PI * 2);
		}

		public SoundPointValue GetValue(double ATime)
		{
			var rv = GetValue(FRFrequency, ATime);
			double lv = rv;

			if (FLFrequencyDelta != 0)
				lv = GetValue(FRFrequency + FLFrequencyDelta, ATime);

			return new SoundPointValue(lv, rv);
		}

		//private void DoRemoveSelf()
		//{
		//	RemoveSelf?.Invoke(this);
		//}
	}
}
