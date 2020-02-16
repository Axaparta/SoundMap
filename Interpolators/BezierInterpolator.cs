using System.ComponentModel;
using System.Linq;

namespace Interpolators
{
	/// <summary>
	/// Аппроксимация кривыми Безье алгоритмом де Кастельжо.
	/// </summary>
	[Description("Кривые Безье")]
	public class BezierInterpolator: Interpolator
	{
		private double[,] b;
		private int n;
		private double minx, maxx;

		protected override void InternalCreateModel()
		{
			n = FXValues.Length;
			b = new double[n, n];
			for (int i = 0; i < n; i++)
			{
				b[0, i] = FYValues[i];
			}
			minx = FXValues.First();
			maxx = FXValues.Last();
		}

		public override double Evaluate(double X)
		{
			double t = (X - minx) / (maxx - minx);
			for (int j = 1; j < n; j++)
			{
				for (int i = 0; i < n - j; i++)
				{
					b[j, i] = b[j-1, i] * (1-t) + b[j-1, i+1] * t;
				}
			}
			return (b[n-1, 0]);
		}
	}
}
