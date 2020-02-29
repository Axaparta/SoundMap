using System;

namespace SoundMap.Temperaments
{
	public struct Fraction
	{
		/// <summary>
		/// Numerator (числитель, верхняя часть дроби)
		/// </summary>
		public int Up { get; }
		/// <summary>
		/// Denominator (знаменатель, нижняя часть дроби)
		/// </summary>
		public int Down { get; }

		public Fraction(int up, int down = 1)
		{
			Up = up;
			Down = down;
		}

		public static Fraction operator *(Fraction a, Fraction b)
		{
			return new Fraction(a.Up * b.Up, a.Down * b.Down);
		}

		public static Fraction operator /(Fraction a, Fraction b)
		{
			return new Fraction(a.Up * b.Down, a.Down * b.Up);
		}

		public static Fraction operator +(Fraction a, Fraction b)
		{
			return new Fraction(a.Up * b.Down + b.Up * a.Down, a.Down * b.Down);
		}

		public static Fraction operator -(Fraction a, Fraction b)
		{
			return new Fraction(a.Up * b.Down - b.Up * a.Down, a.Down * b.Down);
		}

		public static implicit operator double (Fraction f) => (double)f.Up / f.Down;

		public static implicit operator Fraction (int d) => new Fraction(d);

		public Fraction Reduce()
		{
			int nod = Nod(Up, Down);
			if (nod != 0)
				return new Fraction(Up / nod, Down / nod);
			return this;
		}

		private static int Nod(int n, int d)
		{
			int temp;
			n = Math.Abs(n);
			d = Math.Abs(d);
			while (d != 0 && n != 0)
			{
				if (n % d > 0)
				{
					temp = n;
					n = d;
					d = temp % d;
				}
				else break;
			}
			if (d != 0 && n != 0)
				return d;
			else
				return 0;
		}

		public override string ToString()
		{
			return $"{Up}/{Down}";
		}
	}
}
