namespace SoundMap.Temperaments
{
	public class CleanTemperament : DiatonicTemperament
	{
		private static readonly Fraction[] CleanTone = 
		{
			new Fraction(1),
			new Fraction(9, 8),
			new Fraction(5, 4),
			new Fraction(4, 3),
			new Fraction(3, 2),
			new Fraction(5, 3),
			new Fraction(15, 8)
		};

		public CleanTemperament(double baseFrequency = 1):
			base("Clean", baseFrequency, CleanTone, 0, GetKbToFraction(DefaulltIntervals))
		{
		}
	}
}
