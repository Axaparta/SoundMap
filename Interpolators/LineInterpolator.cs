using System;
using System.ComponentModel;

namespace Interpolators
{
	/// <summary>
	/// Интерполяция ломанными линиями
	/// </summary>
	[Description("Ломанные линии")]
	public class LineInterpolator: Interpolator
	{
		private double[] k = null;
		private double[] b = null;

		protected override void InternalCreateModel()
		{
			k = new double[FXValues.Length - 1];
			b = new double[k.Length];
			for (int i = 0; i < b.Length; i++)
			{
				k[i] = (FYValues[i+1] - FYValues[i])/(FXValues[i+1] - FXValues[i]);
				b[i] = FYValues[i] - k[i]*FXValues[i];
			}
		}

		public override double Evaluate(double X)
		{
			if ((k == null) || (b == null))
				throw new Exception("Модель не создана!");
			if (FXValues.Length == 1)
				return FYValues[0];
			int i;
			int n = FXValues.Length;
			if (X <= FXValues[1])
				i = 0;
			else
				if (X >= FXValues[n - 2])
					i = n - 2;
				else
				{
					for (i = 1; i < n - 1; i++)
						if ((X >= FXValues[i]) && (X <= FXValues[i+1]))
							break;
				}
			return b[i] + k[i]*X;
		}
	}
}
