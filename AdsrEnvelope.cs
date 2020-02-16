using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using SoundMap.NoteWaveProviders;

namespace SoundMap
{
	[Serializable]
	public class AdsrEnvelope: ICloneable
	{
		private const double Epsilon = 0.01;
		private const double EpsilonPlus = 1 + Epsilon;

		private double FAttakK;
		private double FAttacTime;
		/// <summary>Multipler отвечает за крутизну изменений (speed of curve)</summary>
		private double FAttacMultipler = 1;

		private double FDecayK;
		private double FDecayTime;
		private double FDecayMultipler = 1;

		private double FReleaseK;
		private double FReleaseTime;
		private double FReleaseMultipler = 1;

		private double FStartTime = double.NaN;
		private double FStopTime = double.NaN;
		private double FStopValue;

		private static Dictionary<string, AdsrEnvelope> FEnvelopes = null;

		public AdsrEnvelope()
		{
		}

		public AdsrEnvelope(double AAttacTime, double ADecayTime, double ASustainLevel, double AReleaseTime)
		{
			AttacTime = AAttacTime;
			DecayTime = ADecayTime;
			SustainLevel = ASustainLevel;
			ReleaseTime = AReleaseTime;
		}

		public static AdsrEnvelope Fast { get; } = new AdsrEnvelope(0.005, 0, 1, 0.005).Clone();
		public static AdsrEnvelope SlowPiano { get; } = new AdsrEnvelope(0.1, 0.2, 0.8, 3).Clone();
		public static AdsrEnvelope Piano { get; } = new AdsrEnvelope(0.005, 5, 0, 0.1).Clone();
		public static AdsrEnvelope Clavisin { get; } = new AdsrEnvelope(0.005, 10, 0, 0) { AttacMultipler = 4, DecayMultipler = 2 }.Clone();
		public static AdsrEnvelope Tube { get; } = new AdsrEnvelope(1, 0, 1, 2).Clone();

		[XmlIgnore]
		public Dictionary<string, AdsrEnvelope> Envelopes
		{
			get
			{
				if (FEnvelopes == null)
				{
					FEnvelopes = new Dictionary<string, AdsrEnvelope>();
					var t = typeof(AdsrEnvelope);
					var props = t.GetProperties(BindingFlags.Public | BindingFlags.Static);
					foreach (var p in props)
						if (p.PropertyType.Equals(t) && !p.PropertyType.IsArray)
							FEnvelopes[p.Name] = ((AdsrEnvelope)p.GetValue(null, null));
				}
				return FEnvelopes;
			}
		}

		public void Start(double ATime)
		{
			FStartTime = ATime;
			FStopTime = double.NaN;
		}

		public bool IsStarted => !double.IsNaN(FStartTime);

		public void Stop(double ATime)
		{
			if (StopIgnore)
				return;
			FStopValue = GetValue(ATime);
			FStopTime = ATime;
		}

		public bool IsStopped => !double.IsNaN(FStopTime);

		public bool IsDone(double ATime) => GetValue(ATime) < Epsilon;

		public double GetValue(double ATime)
		{
			if (double.IsNaN(FStartTime))
				return 0;
			double t;
			if (double.IsNaN(FStopTime))
			{
				t = ATime - FStartTime;
				if (t < FAttacTime)
					return GetUpTo(t, FAttakK);
				t -= FAttacTime;
				if (t < FDecayTime)
					return 1 - (1 - SustainLevel) * GetUpTo(t, FDecayK);
				//return SustainLevel + (1 - SustainLevel) * GetDownTo(t, FDecayK);
				return SustainLevel;
			}
			t = ATime - FStopTime;
			if (t < FReleaseTime)
				return FStopValue * GetDownTo(t, FReleaseK);
			return 0;
		}

		private double GetUpTo(double ATime, double AK)
		{
			return (1 - 1 / (1 + AK * ATime * ATime)) * EpsilonPlus;
		}

		private double GetDownTo(double ATime, double AK)
		{
			return EpsilonPlus / (1 + AK * ATime * ATime) - Epsilon;
		}

		private static double GetK(double ATime)
		{
			if (ATime == 0)
				return 0;
			return (1 - Epsilon) / Epsilon / ATime / ATime;
		}

		public double AttacTime
		{
			get => FAttacTime;
			set
			{
				FAttacTime = value;
				UpdateAttacK();
			}
		}

		public double AttacMultipler
		{
			get => FAttacMultipler;
			set
			{
				FAttacMultipler = value;
				UpdateAttacK();
			}
		}

		private void UpdateAttacK() => FAttakK = FAttacMultipler * GetK(FAttacTime);

		public double DecayTime
		{
			get => FDecayTime;
			set
			{
				FDecayTime = value;
				UpdateDecayK();
			}
		}

		public double DecayMultipler
		{
			get => FDecayMultipler;
			set
			{
				FDecayMultipler = value;
				UpdateDecayK();
			}
		}

		private void UpdateDecayK() => FDecayK = FDecayMultipler * GetK(FDecayTime);

		public double SustainLevel
		{
			get;
			set;
		}

		public double ReleaseMultipler
		{
			get => FReleaseMultipler;
			set
			{
				FReleaseMultipler = value;
				UpdateReleaseK();
			}
		}

		public void UpdateReleaseK() => FReleaseK = FReleaseMultipler * GetK(FReleaseTime);

		/// <summary>
		/// ReleaseTime == 0 - ignore Stop, decay only
		/// </summary>
		public double ReleaseTime
		{
			get => FReleaseTime;
			set
			{
				FReleaseTime = value;
				UpdateReleaseK();
			}
		}

		public bool StopIgnore => FReleaseTime == 0;

		public OpenCLEnvelope GetOpenCL()
		{
			return new OpenCLEnvelope()
			{
				attakK = (float)FAttakK,
				attacTime = (float)FAttacTime,

				decayK = (float)FDecayK,
				decayTime = (float)FDecayTime,

				releaseK = (float)FReleaseK,
				releaseTime = (float)FReleaseTime,

				startTime = (double.IsNaN(FStartTime) ? -1 : (float)FStartTime),
				stopTime = (double.IsNaN(FStopTime) ? -1 : (float)FStopTime),
				stopValue = (float)FStopValue,
				sustainLevel = (float)SustainLevel
			};
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public AdsrEnvelope Clone()
		{
			var r = (AdsrEnvelope)MemberwiseClone();
			r.SustainLevel = this.SustainLevel;
			return r;
		}

		public override int GetHashCode()
		{
			return FAttakK.GetHashCode() ^ FDecayK.GetHashCode() ^ SustainLevel.GetHashCode() ^ FReleaseK.GetHashCode();
		}
	}
}
