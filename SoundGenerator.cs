using NAudio.Wave;
using System;
using System.Linq;

namespace SoundMap
{
	public class SoundGenerator : ISampleProvider
	{
		public WaveFormat WaveFormat { get; }
		private double FTime = 0;
		private SoundPoint[] FNewPoints = { };
		private readonly object FPointsLock = new object();
		private SoundPoint[] FCurrentPoints = { };

		public SoundGenerator(WaveFormat AWaveFormat)
		{
			WaveFormat = AWaveFormat;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int maxn = offset + count;
			int maxOldIndex = 60;
			int oldIndex = maxOldIndex;

			SoundPoint[] oldPoints = null;

			lock (FPointsLock)
			{
				if (FNewPoints != null)
				{
					oldPoints = FCurrentPoints;
					FCurrentPoints = FNewPoints;
					FNewPoints = null;

					if (oldPoints == null)
						oldPoints = new SoundPoint[] { new SoundPoint(0, 0) };
				}
			}

			switch (WaveFormat.Channels)
			{
				case 1:
					for (int n = offset; n < maxn; n++)
					{
						buffer[n] = (float)GetValue(FCurrentPoints, FTime);

						if (oldPoints != null)
						{
							var op = (float)GetValue(oldPoints, FTime);
							var oldPct = (float)oldIndex / (float)maxOldIndex;
							buffer[n] = op * oldPct + buffer[n] * (1 - oldPct);
						}

						FTime += 1 / (double)WaveFormat.SampleRate;
						oldIndex--;
						if (oldIndex < 0)
							oldPoints = null;
					}
					break;
				case 2:
					for (int n = offset; n < maxn; n++)
					{
						var v = (float)GetValue(FCurrentPoints, FTime);

						if (oldPoints != null)
						{
							var op = (float)GetValue(oldPoints, FTime);
							var oldPct = (float)oldIndex / (float)maxOldIndex;
							v = op * oldPct + v * (1 - oldPct);
						}

						buffer[n] = v;
						n++;
						buffer[n] = v;

						FTime += 1 / (double)WaveFormat.SampleRate;
						oldIndex--;
						if (oldIndex < 0)
							oldPoints = null;
					}
					break;
				default:
					throw new Exception(string.Format("Неверное количество каналов: {0}", WaveFormat.Channels));
			}
			return count;
		}

		public double GetValue(SoundPoint[] APoints, double ATime)
		{
			double max = 0;
			double r = 0;
			foreach (var p in APoints)
			{
				max += p.Volume;
				r += p.Volume * Math.Sin(p.Frequency * ATime * Math.PI * 2);
			}

			if (max > 1)
				r = r / max;

			//return Math.Sin(440 * ATime * Math.PI * 2);
			return r;
		}

		public void SetPoints(SoundPointCollection APoints)
		{
			lock (FPointsLock)
			{
				FNewPoints = APoints.Select(p => p.Clone()).ToArray();
			}
		}
	}
}
