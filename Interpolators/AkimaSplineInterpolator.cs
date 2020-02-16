using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Interpolators
{
	/// <summary>
	/// Интерполяция сплайнами Акима
	/// <remarks>Стянуто из пакета aspline</remarks>
	/// </summary>
	[Description("Сплайны Акима")]
	public class AkimaSplineInterpolator : Interpolator
	{
		private double[] x = null;
		private double[] y = null;
		private double[] dx = null;
		private double[] dy = null;
		private double[] m = null;
		private double[] t = null;
		private double[] C = null;
		private double[] D = null;
		private double k, b;

		public override double Evaluate(double X)
		{
			switch (FXValues.Length)
			{
				case 1:
					return FYValues[0];
				case 2:
					return k * X + b;
				default:
					// calculate the intermediate values 
					int n = x.Length;
					int p;
					for (p = 3; p < n - 2; p++)
						if (X <= x[p])
							break;
					double xd = X - x[p - 1];
					return y[p - 1] + (t[p - 1] + (C[p - 1] + D[p - 1] * xd) * xd) * xd;
			}
		}

		protected override void InternalCreateModel()
		{
			InternalCreateModelNonParallel();
			//InternalCreateModelParallel();
		}

		protected void InternalCreateModelNonParallel()
		{
			switch (FXValues.Length)
			{
				case 0:
				case 1:
					return;
				case 2:
					k = (FYValues[1] - FYValues[0]) / (FXValues[1] - FXValues[0]);
					b = FYValues[0] - k * FXValues[0];
					return;
				default:
					// Leading extrapolation points, actual values will be filled in later 
					x = new double[FXValues.Length + 4];
					y = new double[x.Length];

					Array.Copy(FXValues, 0, x, 2, FXValues.Length);
					Array.Copy(FYValues, 0, y, 2, FYValues.Length);

					int n = x.Length;

					// calculate coefficients of the spline (Akima interpolation itself)
					dx = new double[n];
					dy = new double[n];
					m = new double[n];
					t = new double[n];
					C = new double[n];
					D = new double[n];

					// a) Calculate the differences and the slopes m[i]. 
					for (int i = 2; i < n - 3; i++)
					{
						dx[i] = x[i + 1] - x[i];
						dy[i] = y[i + 1] - y[i];
						m[i] = dy[i] / dx[i];
					}

					// b) interpolate the missing points: 
					x[1] = x[2] + x[3] - x[4];
					dx[1] = x[2] - x[1];

					y[1] = dx[1] * (m[3] - 2 * m[2]) + y[2];
					dy[1] = y[2] - y[1];

					m[1] = dy[1] / dx[1];

					x[0] = 2 * x[2] - x[4];
					dx[0] = x[1] - x[0];

					y[0] = dx[0] * (m[2] - 2 * m[1]) + y[1];
					dy[0] = y[1] - y[0];

					m[0] = dy[0] / dx[0];

					x[n - 2] = x[n - 3] + x[n - 4] - x[n - 5];
					y[n - 2] = (2 * m[n - 4] - m[n - 5]) * (x[n - 2] - x[n - 3]) + y[n - 3];

					x[n - 1] = 2 * x[n - 3] - x[n - 5];
					y[n - 1] = (2 * m[n - 3] - m[n - 4]) * (x[n - 1] - x[n - 2]) + y[n - 2];

					for (int i = n - 3; i < n - 1; i++)
					{
						dx[i] = x[i + 1] - x[i];
						dy[i] = y[i + 1] - y[i];
						m[i] = dy[i] / dx[i];
					}

					// the first x slopes and the last y ones are extrapolated: 
					t[0] = 0;
					t[1] = 0;  // not relevant 
					for (int i = 2; i < n - 2; i++)
					{
						double num, den;

						num = Math.Abs(m[i + 1] - m[i]) * m[i - 1] + Math.Abs(m[i - 1] - m[i - 2]) * m[i];
						den = Math.Abs(m[i + 1] - m[i]) + Math.Abs(m[i - 1] - m[i - 2]);

						if (den != 0)
							t[i] = num / den;
						else
							t[i] = 0.0;
					}

					// c) Allocate the polynom coefficients 
					for (int i = 2; i < n - 2; i++)
					{
						C[i] = (3 * m[i] - 2 * t[i] - t[i + 1]) / dx[i];
						D[i] = (t[i] + t[i + 1] - 2 * m[i]) / (dx[i] * dx[i]);
					}

					break;
			}
		}

		protected void InternalCreateModelParallel()
		{
			switch (FXValues.Length)
			{
				case 0:
				case 1:
					return;
				case 2:
					k = (FYValues[1] - FYValues[0]) / (FXValues[1] - FXValues[0]);
					b = FYValues[0] - k * FXValues[0];
					return;
				default:
					// Leading extrapolation points, actual values will be filled in later 
					x = new double[FXValues.Length + 4];
					y = new double[x.Length];

					Array.Copy(FXValues, 0, x, 2, FXValues.Length);
					Array.Copy(FYValues, 0, y, 2, FYValues.Length);

					int n = x.Length;

					// calculate coefficients of the spline (Akima interpolation itself)
					dx = new double[n];
					dy = new double[n];
					m = new double[n];
					t = new double[n];
					C = new double[n];
					D = new double[n];

					// a) Calculate the differences and the slopes m[i]. 
					Parallel.For(2, n - 3, (i) =>
					{
						dx[i] = x[i + 1] - x[i];
						dy[i] = y[i + 1] - y[i];
						m[i] = dy[i] / dx[i];
					});

					// b) interpolate the missing points: 
					x[1] = x[2] + x[3] - x[4];
					dx[1] = x[2] - x[1];

					y[1] = dx[1] * (m[3] - 2 * m[2]) + y[2];
					dy[1] = y[2] - y[1];

					m[1] = dy[1] / dx[1];

					x[0] = 2 * x[2] - x[4];
					dx[0] = x[1] - x[0];

					y[0] = dx[0] * (m[2] - 2 * m[1]) + y[1];
					dy[0] = y[1] - y[0];

					m[0] = dy[0] / dx[0];

					x[n - 2] = x[n - 3] + x[n - 4] - x[n - 5];
					y[n - 2] = (2 * m[n - 4] - m[n - 5]) * (x[n - 2] - x[n - 3]) + y[n - 3];

					x[n - 1] = 2 * x[n - 3] - x[n - 5];
					y[n - 1] = (2 * m[n - 3] - m[n - 4]) * (x[n - 1] - x[n - 2]) + y[n - 2];

					Parallel.For(n - 3, n - 1, (i) =>
					{
						dx[i] = x[i + 1] - x[i];
						dy[i] = y[i + 1] - y[i];
						m[i] = dy[i] / dx[i];
					});

					// the first x slopes and the last y ones are extrapolated: 
					t[0] = 0;
					t[1] = 0;  // not relevant 
					Parallel.For(2, n - 2, (i) =>
					{
						var num = Math.Abs(m[i + 1] - m[i]) * m[i - 1] + Math.Abs(m[i - 1] - m[i - 2]) * m[i];
						var den = Math.Abs(m[i + 1] - m[i]) + Math.Abs(m[i - 1] - m[i - 2]);

						if (den != 0)
							t[i] = num / den;
						else
							t[i] = 0.0;
					});

					// c) Allocate the polynom coefficients 
					Parallel.For(2, n - 2, (i) =>
					{
						C[i] = (3 * m[i] - 2 * t[i] - t[i + 1]) / dx[i];
						D[i] = (t[i] + t[i + 1] - 2 * m[i]) / (dx[i] * dx[i]);
					});

					break;
			}
		}
	}
}
