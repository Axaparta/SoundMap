using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SoundMap.Temperaments
{
	public enum PifagorTemperamentScale
	{
		Lidiy,
		Frigiy,
		Doriy,
		HyperDoriy,
		HypoLidiy,
		HypoFrigiy,
		HypoDoriy
	}

	public class PifagorTemperament : DiatonicTemperament
	{
		private static readonly Interval[] LidiyIntervals = DefaulltIntervals;
		private static readonly Interval[] FrigiyIntervals = { Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.Tone, Interval.HalfTone };
		private static readonly Interval[] DoriyIntervals = new[] { Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone };
		private static readonly Interval[] HyperDoriyIntervals = new[] { Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone };
		private static readonly Interval[] HypoLidiyIntervals = new[] { Interval.Tone, Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone };
		private static readonly Interval[] HypoFrigiyIntervals = new[] { Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.HalfTone };
		private static readonly Interval[] HypoDoriyIntervals = new[] { Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone };

		public PifagorTemperament(double baseFrequency = 1, PifagorTemperamentScale scale = PifagorTemperamentScale.Lidiy):
			base(
				"Pifagor " + scale.ToString(),
				baseFrequency,
				GetFractionByTones(scale),
				GetFractionOffset(scale),
				GetKbToFraction(GetScaleInterval(scale)))
		{
		}

		private static Interval[] GetScaleInterval(PifagorTemperamentScale scale)
		{
			switch (scale)
			{
				case PifagorTemperamentScale.Lidiy:
					return LidiyIntervals;
				case PifagorTemperamentScale.Frigiy:
					return FrigiyIntervals;
				case PifagorTemperamentScale.Doriy:
					return DoriyIntervals;
				case PifagorTemperamentScale.HyperDoriy:
					return HyperDoriyIntervals;
				case PifagorTemperamentScale.HypoLidiy:
					return HypoLidiyIntervals;
				case PifagorTemperamentScale.HypoFrigiy:
					return HypoFrigiyIntervals;
				case PifagorTemperamentScale.HypoDoriy:
					return HypoDoriyIntervals;
				default:
					throw new NotImplementedException();
			}
		}

		private static int GetFractionOffset(PifagorTemperamentScale scale)
		{
			switch (scale)
			{
				case PifagorTemperamentScale.Lidiy:
					return 0;
				case PifagorTemperamentScale.Frigiy:
					return 2;
				case PifagorTemperamentScale.Doriy:
					return 4;
				case PifagorTemperamentScale.HyperDoriy:
					return -1;
				case PifagorTemperamentScale.HypoLidiy:
					return 5;
					break;
				case PifagorTemperamentScale.HypoFrigiy:
					//ktfOffset = -5;
					return 7;
				case PifagorTemperamentScale.HypoDoriy:
					//ktfOffset = -3;
					return 9;
				default:
					throw new NotImplementedException();
			}
		}

		private static Fraction[] GetFractionByTones(PifagorTemperamentScale scale)
		{
			var tonica = GetScaleInterval(scale);
			List<Fraction> r = new List<Fraction>(tonica.Length + 1);

			r.Add(1);

			for (int i = 0; i < tonica.Length; i++)
			{
				var f = r.Last();
				switch (tonica[i])
				{
					case Interval.Tone:
						f *= new Fraction(9, 8);
						break;
					case Interval.HalfTone:
						f *= new Fraction(256, 243);
						break;
				}
				r.Add(f.Reduce());
			}

			return r.ToArray();
		}
	}
}
