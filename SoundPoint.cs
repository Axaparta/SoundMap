
using System;
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
		private double FStartTime = double.NaN;

		private double FVolume = 0;
		private double FFrequency = 0;

		private double FNewFrequency = double.NaN;
		private double FOldFrequency = double.NaN;
		private double FTimeOffset = 0; 

		private double FTransitionTimeStart = double.NaN;
		private double FTransitionTimeLength = double.NaN;

		public event Action<SoundPoint> RemoveSelf;

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

		public double Frequency
		{
			get => FFrequency;
			set
			{
				if (FFrequency != value)
				{
					FOldFrequency = FFrequency;
					FNewFrequency = value;
					FFrequency = value;
					NotifyPropertyChanged(nameof(Frequency));
				}
			}
		}

		public override string ToString()
		{
			return Frequency.ToString("F2");
		}

		public double GetValue(double ATime)
		{
			var oldValue = Math.Sin(FOldFrequency * (ATime - FTimeOffset) * Math.PI * 2);

			if (!double.IsNaN(FNewFrequency))
			{
				if (double.IsNaN(FTransitionTimeStart))
				{
					FTransitionTimeLength = 4 / FNewFrequency;
					FTransitionTimeStart = ATime;
				}

				var newValue = Math.Sin(FNewFrequency * (ATime - FTransitionTimeStart) * Math.PI * 2);

				var transPct = (ATime - FTransitionTimeStart) / FTransitionTimeLength;

				oldValue = (1 - transPct) * oldValue + transPct * newValue;
				if (ATime - FTransitionTimeStart >= FTransitionTimeLength)
				{
					FTimeOffset = FTransitionTimeStart;
					FOldFrequency = FNewFrequency;
					FNewFrequency = double.NaN;
					FTransitionTimeStart = double.NaN;
				}
			}

			return oldValue;

			//switch (Kind)
			//{
			//	case PointKind.Static:
			//		if (!double.IsNaN(FOldFrequency) && (FOldFrequency != 0))
			//		{
			//			// Получаю значение каким бы оно должно быть
			//			var oldV = Math.Sin(FOldFrequency * ATime * Math.PI * 2 + FPhaseOffset);
			//			// Вычисление угла чтобы текущее значение...
			//			FPhaseOffset = Math.Asin(oldV) - 2 * Math.PI * Frequency * ATime;
			//			FOldFrequency = double.NaN;
			//		}
			//		return Math.Sin(Frequency * ATime * Math.PI * 2 + FPhaseOffset);
			//	case PointKind.Bell:
			//		if (double.IsNaN(FStartTime))
			//			FStartTime = ATime;

			//		var d = ATime - FStartTime;
			//		d = 1 / (1 + d*d);

			//		if (d < 0.1)
			//			DoRemoveSelf();

			//		return  d*Math.Sin(Frequency * ATime * Math.PI * 2);
			//	case PointKind.Saw:
			//		// Частота - это количество колебаний в секунду. Колебание необходимо разделить на 3 фазы.
			//		// А для этого нужен вещественный остаток времени фазы
			//		var z = Math.Truncate(ATime * Frequency);
			//		var rem = ATime - z/ Frequency;
			//		// 1/Frequency время одного колебания 1/F/4 - время одной фазы
			//		var f = 1 / Frequency;
			//		var f4 = f / 4;
			//		if (rem < f4)
			//			return rem/f4;
			//		if (rem < f4 * 3)
			//		{
			//			rem -= f4;
			//			return 1 - rem / f4;
			//		}
			//		rem -= f4 * 3;
			//		return -1 + rem / f4;
			//	default:
			//		throw new NotImplementedException();
			//}
		}

		private void DoRemoveSelf()
		{
			RemoveSelf?.Invoke(this);
		}
	}
}
