
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SoundMap
{
	public delegate void SoundPointEvent(SoundPoint APoint);

	public struct SoundPointValue
	{
		public double Left { get; set; }
		public double Right { get; set; }

		public SoundPointValue(double ALeft, double ARight)
		{
			Left = ALeft;
			Right = ARight;
		}

		public static SoundPointValue operator *(double AMultipler, SoundPointValue AValue)
		{
			return new SoundPointValue(AMultipler * AValue.Left, AMultipler * AValue.Right);
		}

		public static SoundPointValue operator *(SoundPointValue AValue, double AMultipler)
		{
			return new SoundPointValue(AMultipler * AValue.Left, AMultipler * AValue.Right);
		}

		public static SoundPointValue operator /(SoundPointValue AValue, double ADivider)
		{
			return new SoundPointValue(AValue.Left/ADivider, AValue.Right/ADivider);
		}

		public static SoundPointValue operator +(SoundPointValue AValueA, SoundPointValue AValueB)
		{
			return new SoundPointValue(AValueA.Left + AValueB.Left, AValueA.Right + AValueB.Right);
		}
	}

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

		private double FNewRFrequency = double.NaN;
		private double FOldRFrequency = double.NaN;

		private double FNewLFrequencyDelta = 0;
		private double FOldLFrequencyDelta = 0;

		private double FTimeOffset = 0; 

		private double FTransitionTimeStart = double.NaN;
		private double FTransitionTimeLength = double.NaN;

		public event Action<SoundPoint> RemoveSelf;

		//public static PointKind[] PointKinds { get; } = Enum.GetValues(typeof(PointKind)).Cast<PointKind>().ToArray();
		public static KeyValuePair<PointKind, string>[] PointKinds { get; } = Enum.GetValues(typeof(PointKind)).Cast<PointKind>().Select(p => new KeyValuePair<PointKind, string>(p, p.ToString())).ToArray();


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
					FOldLFrequencyDelta = FLFrequencyDelta;
					FNewLFrequencyDelta = value;
					FLFrequencyDelta = value;
					FNewRFrequency = FRFrequency;
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
					FOldRFrequency = FRFrequency;
					FNewRFrequency = value;
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
			var oldRValue = GetValue(FOldRFrequency, ATime - FTimeOffset);
			var oldLValue = GetValue(FOldRFrequency + FOldLFrequencyDelta, ATime - FTimeOffset);

			if (!double.IsNaN(FNewRFrequency))
			{
				if (double.IsNaN(FTransitionTimeStart))
				{
					FTransitionTimeLength = 4 / FNewRFrequency;
					FTransitionTimeStart = ATime;
				}

				var newRValue = GetValue(FNewRFrequency, ATime - FTransitionTimeStart);
				var newLValue = GetValue(FNewRFrequency + FNewLFrequencyDelta, ATime - FTransitionTimeStart);

				var transPct = (ATime - FTransitionTimeStart) / FTransitionTimeLength;

				oldRValue = (1 - transPct) * oldRValue + transPct * newRValue;
				oldLValue = (1 - transPct) * oldLValue + transPct * newLValue;

				if (ATime - FTransitionTimeStart >= FTransitionTimeLength)
				{
					FTimeOffset = FTransitionTimeStart;
					FTransitionTimeStart = double.NaN;

					FOldRFrequency = FNewRFrequency;
					FNewRFrequency = double.NaN;

					FOldLFrequencyDelta = FNewLFrequencyDelta;
				}
			}

			return new SoundPointValue(oldLValue, oldRValue);

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
