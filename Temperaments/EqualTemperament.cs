using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SoundMap.Temperaments
{
	public class EqualTemperamentScaleAttribute: Attribute
	{
		public TemperamentMode Mode { get; }
		public int SignOffset { get; }

		public EqualTemperamentScaleAttribute(TemperamentMode mode, int signOffset)
		{
			Mode = mode;
			SignOffset = signOffset;
		}

		public static EqualTemperamentScaleAttribute GetAttributeFrom(Enum enumVal)
		{
			var type = enumVal.GetType();
			var memInfo = type.GetMember(enumVal.ToString());
			var attributes = memInfo[0].GetCustomAttributes(typeof(EqualTemperamentScaleAttribute), false);
			return (attributes.Length > 0) ? (EqualTemperamentScaleAttribute)attributes[0] : null;
		}
	}

	public enum EqualTemperamentScale
	{ 
		All,

		[EqualTemperamentScale(TemperamentMode.Major, 0)]
		DoMajor,

		[EqualTemperamentScale(TemperamentMode.Minor, 0)]
		DoMinor,

		[EqualTemperamentScale(TemperamentMode.Minor, -3)]
		LaMinor,

		[EqualTemperamentScale(TemperamentMode.Major, 1)]
		DoDiesMajor
	}

	public class EqualTemperament: Temperament
	{
		private static readonly int[] FullTonica = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
		private static readonly int[] MajorTonica = { 0, 2, 4, 5, 7, 9, 11 };
		private static readonly int[] MinorTonica = { 0, 2, 3, 5, 7, 8, 10 };
		private static readonly string[] ScaleSign = { "Do", "Do#", "Re", "Re#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

		private static readonly int[] FillKeyboard = FullTonica;
		private static readonly int[] MajorKeyboard = { 0, int.MaxValue, 2, int.MaxValue, 4, 5, int.MaxValue, 7, int.MaxValue, 9, int.MaxValue, 11 };
		private static readonly int[] MinorKeyboard = { 0, int.MaxValue, 2, 3, int.MaxValue, 5, int.MaxValue, 7, 8, int.MaxValue, 10, int.MaxValue };

		private readonly EqualTemperamentScale scale;
		private readonly EqualTemperamentScaleAttribute scaleAttr;

		public EqualTemperament(double baseFrequency = 1, EqualTemperamentScale scale = EqualTemperamentScale.All)
			: base("Equal " + scale.ToString(), baseFrequency)
		{
			this.scale = scale;
			scaleAttr = EqualTemperamentScaleAttribute.GetAttributeFrom(scale);
		}

		public override Tone[] GetTemperamentTones(int fromOffset, int toOffset)
		{
			List<Tone> r = new List<Tone>(toOffset - fromOffset + 1);

			if (scale == EqualTemperamentScale.All)
				for (int i = fromOffset; i <= toOffset; i++)
					r.Add(new Tone(BaseFrequency * Math.Pow(2, (double)i / 12), i, $"{i}/12"));
			else
			{
				Debug.Assert(scaleAttr != null, "EqualTemperamentScale attr");

				int[] tonica;

				switch (scaleAttr.Mode)
				{
					case TemperamentMode.Major:
						tonica = MajorTonica;
						break;
					case TemperamentMode.Minor:
						tonica = MinorTonica;
						break;
					default:
						throw new NotImplementedException();
				}

				var len = tonica.Length;

				for (int i = fromOffset; i <= toOffset; i++)
				{
					int t = i % len;
					int oct = i / len;
					if ((t != 0) && (i < 0))
						oct--;

					if (t < 0)
						t += len;

					var pt = oct * 12 + tonica[t];
					r.Add(new Tone(BaseFrequency * Math.Pow(2, pt / 12D), i, $"{oct} {ScaleSign[(scaleAttr.SignOffset + tonica[t]) % ScaleSign.Length]}"));
				}

			}

			return r.ToArray();
		}

		public override Tone GetKeyboardTone(int halfToneOffset)
		{
			int[] keyb;

			if (scaleAttr == null)
			{
				keyb = FillKeyboard;
			}
			else
				switch (scaleAttr.Mode)
				{
					case TemperamentMode.Major:
						keyb = MajorKeyboard;
						break;
					case TemperamentMode.Minor:
						keyb = MinorKeyboard;
						break;
					default:
						throw new NotImplementedException();
				}

			//if ((scaleAttr != null) && (scaleAttr.Mode == TemperamentMode.Minor))
			//	Console.WriteLine();

			// Основной тон смещён от начала клавиатуры. Поэтому 0 клавиатуры, это -N по отношению к основному тону
			int signOffset = halfToneOffset - ((scaleAttr == null) ? 0 : scaleAttr.SignOffset);
			int kbOffset = signOffset % 12;
			if (kbOffset < 0)
				kbOffset += 12;

			// Полутоновое смещение относительно основного тона, но без учёта октавы
			int kbtone = keyb[kbOffset];

			if (kbtone == int.MaxValue)
				return null;

			int oct = signOffset / keyb.Length;
			if ((signOffset < 0) && (signOffset % keyb.Length != 0))
				oct--;

			var pt = oct * 12 + kbtone;
			return new Tone(BaseFrequency * Math.Pow(2, pt / 12D), halfToneOffset, $"k {halfToneOffset} ^ {pt}");
		}
	}
}
