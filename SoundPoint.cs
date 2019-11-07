
using System;
using System.Windows;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundPoint : Observable, ICloneable
	{
		// Желаемый диапазон частот
		private const double FMin = 80;
		private const double FMax = 2000;
		// Горизонтальная шкала - степень двойки (степень линейна). Далее максимальные и минимальные значения этой шкалы
		private readonly double LogFMin = Math.Log(FMin, 2);
		private readonly double LogFMax = Math.Log(FMax, 2);

		private Point FRelative = new Point(0.5, 0.5);
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

		public SoundPoint(double ARelativeX, double ARelativeY)
		{
			Relative = new Point(ARelativeX, ARelativeY);
		}

		public SoundPoint(Point ARelative)
		{
			Relative = ARelative;
		}

		public Point Relative
		{
			get => FRelative;
			set
			{
				if (FRelative != value)
				{
					FRelative = value;
					if (FRelative.X < 0)
						FRelative.X = 0;
					if (FRelative.Y < 0)
						FRelative.Y = 0;
					if (FRelative.X > 1)
						FRelative.X = 1;
					if (FRelative.Y > 1)
						FRelative.Y = 1;

					var pow = LogFMin + (LogFMax - LogFMin) * FRelative.X;
					Frequency = Math.Pow(2, pow);

					NotifyPropertyChanged(() => Relative);
				}
			}
		}

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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public SoundPoint Clone()
		{
			return new SoundPoint(FRelative);
		}

		public double Volume => 1 - FRelative.Y;

		[XmlIgnore]
		public double Frequency { get; private set; } = 0;

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
