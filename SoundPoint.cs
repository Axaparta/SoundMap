
using System;
using System.Windows;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundPoint : Observable
	{
		//// Желаемый диапазон частот
		//private const double FMin = 80;
		//private const double FMax = 800;
		//// Горизонтальная шкала - степень двойки (степень линейна). Далее максимальные и минимальные значения этой шкалы
		//private readonly double LogFMin = Math.Log(FMin, 2);
		//private readonly double LogFMax = Math.Log(FMax, 2);

		private bool FIsSelected = false;

		private bool FIsSolo = false;
		private bool FIsMute = false;
		private PointKind FKind = PointKind.Static;
		private double FStartTime = double.NaN;

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

		/// <summary>
		/// Необходима для группового перемещения
		/// </summary>
		[XmlIgnore]
		public Point StartRelative { get; set; }

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		//public SoundPoint Clone()
		//{
		//	return new SoundPoint(FRelative);
		//}

		public double Volume { get; set; } = 0;
		public double Frequency { get; set; } = 0;

		public override string ToString()
		{
			return Frequency.ToString("F2");
		}

		public double GetValue(double ATime)
		{
			switch (Kind)
			{
				case PointKind.Static:
					return Math.Sin(Frequency * ATime * Math.PI * 2);
				case PointKind.Bell:
					if (double.IsNaN(FStartTime))
						FStartTime = ATime;

					var d = ATime - FStartTime;
					d = 1 / (1 + d*d);

					if (d < 0.1)
						DoRemoveSelf();

					return  d*Math.Sin(Frequency * ATime * Math.PI * 2);
				case PointKind.Saw:
					// Частота - это количество колебаний в секунду. Колебание необходимо разделить на 3 фазы.
					// А для этого нужен вещественный остаток времени фазы
					var z = Math.Truncate(ATime * Frequency);
					var rem = ATime - z/ Frequency;
					// 1/Frequency время одного колебания 1/F/4 - время одной фазы
					var f = 1 / Frequency;
					var f4 = f / 4;
					if (rem < f4)
						return rem/f4;
					if (rem < f4 * 3)
					{
						rem -= f4;
						return 1 - rem / f4;
					}
					rem -= f4 * 3;
					return -1 + rem / f4;
				default:
					throw new NotImplementedException();
			}
		}

		private void DoRemoveSelf()
		{
			RemoveSelf?.Invoke(this);
		}
	}
}
