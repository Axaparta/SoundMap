using System.Collections.Generic;
using System.Diagnostics;

namespace SoundMap.Temperaments
{
	public abstract class DiatonicTemperament: Temperament
	{
		protected static readonly string[] DiatonicSign = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Si" };
		protected static readonly Interval[] DefaulltIntervals = { Interval.Tone, Interval.Tone, Interval.HalfTone, Interval.Tone, Interval.Tone, Interval.Tone };

		protected readonly Fraction[] tones;
		protected readonly int[] kbToFraction;
		protected readonly int fractionOffset;

		protected DiatonicTemperament(string name, double baseFrequency, Fraction[] tones, int fractionOffset, int[] kbToFraction) :
			base(name, baseFrequency)
		{
			this.tones = tones;
			this.fractionOffset = fractionOffset;
			this.kbToFraction = kbToFraction;
		}

		protected virtual string GetToneName(int octave, int offset, Fraction fraction)
		{
			if (octave == 0)
				return DiatonicSign[offset];
			return $"{octave}{DiatonicSign[offset]}";
		}

		public override Tone[] GetTemperamentTones(int fromOffset, int toOffset)
		{
			List<Tone> r = new List<Tone>(toOffset - fromOffset + 1);
			

			int len = tones.Length;

			for (int i = fromOffset; i <= toOffset; i++)
			{
				int t = i % len;
				int oct = i / len;
				if ((t != 0) && (i < 0))
					oct--;

				double mult = 1;
				if (i > 0)
					mult = 1 << oct;
				else if (i < 0)
					mult = 1D / (1 << -oct);

				if (t < 0)
					t += len;

				var n = GetToneName(oct, t, tones[t]);
				r.Add(new Tone(BaseFrequency * mult * tones[t], i, n));
			}

			return r.ToArray();
		}

		protected static int[] GetKbToFraction(Interval[] intervals)
		{
			int kbIndex = 0;
			int[] kbtf = new int[12];
			kbtf[kbIndex] = 0;
			int frIndex = 0;
			for (int i = 0; i < intervals.Length; i++)
			{
				if (intervals[i] == Interval.HalfTone)
				{
					kbtf[++kbIndex] = ++frIndex;
				}
				else
				{
					kbtf[++kbIndex] = int.MaxValue;
					kbtf[++kbIndex] = ++frIndex;
				}
			}
			kbIndex++;
			if (kbIndex < kbtf.Length)
				kbtf[kbIndex] = int.MaxValue;
			return kbtf;
		}

		public override Tone GetKeyboardTone(int halfToneOffset)
		{
			// Основной тон смещён от начала клавиатуры. Поэтому 0 клавиатуры, это -N по отношению к основному тону
			int signOffset = halfToneOffset - fractionOffset;
			int kbOffset = signOffset % 12;
			if (kbOffset < 0)
				kbOffset += 12;

			// Полутоновое смещение относительно основного тона, но без учёта октавы
			int kbtone = kbToFraction[kbOffset];

			if (kbtone == int.MaxValue)
				return null;

			int oct = signOffset / kbToFraction.Length;
			if ((signOffset < 0) && (signOffset % kbToFraction.Length != 0))
				oct--;

			double mult = 1;
			if (signOffset > 0)
				mult = 1 << oct;
			else if (signOffset < 0)
				mult = 1D / (1 << -oct);

			//var n = GetToneName(oct, t, tones[t]);
			return new Tone(BaseFrequency * mult * tones[kbtone], halfToneOffset, $"{halfToneOffset} {kbtone}");
		}
	}
}
