using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SoundMap
{
	public abstract class Waveform: ICloneable
	{
		protected const double TwoPi = 2 * Math.PI;

		public int SampleRate { get; private set; }
		public abstract string Name { get; }

		/// <summary>
		/// Need for group init
		/// </summary>
		public List<Waveform> LinkedWaveforms { get; private set; } = new List<Waveform>();

		public virtual void Init(int ASampleRate)
		{
			SampleRate = ASampleRate;
			foreach (var lwf in LinkedWaveforms)
				lwf.Init(ASampleRate);
		}

		public abstract double GetValue(double ATime, double AFrequency);

		public virtual Waveform Clone()
		{
			var r = (Waveform)MemberwiseClone();
			r.LinkedWaveforms = new List<Waveform>();
			r.SampleRate = this.SampleRate;
			return r;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return obj.GetHashCode() == this.GetHashCode();
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}

	public class SineWaveform: Waveform
	{
		public override string Name => "Sine";
		public double offset;

		public override double GetValue(double ATime, double AFrequency)
		{
			return Math.Sin(TwoPi * ATime * AFrequency + offset);
		}

		public override Waveform Clone()
		{
			var r = (SineWaveform)base.Clone();
			Random rnd = new Random();
			r.offset = TwoPi * rnd.NextDouble();
			return r;
		}
	}

	public abstract class BufferWaveForm : Waveform
	{
		private struct WaveBufferKey
		{
			public Type WaveformType { get; }
			public int SampleRate { get; }

			public WaveBufferKey(Type AWaveformType, int ASampleRate)
			{
				WaveformType = AWaveformType;
				SampleRate = ASampleRate;
			}

			public override string ToString()
			{
				return $"{WaveformType.Name}: {SampleRate}";
			}
		}

		private static readonly Dictionary<WaveBufferKey, double[]> FSamples = new Dictionary<WaveBufferKey, double[]>();
		protected double[] FSample = null;

		public override void Init(int ASampleRate)
		{
			base.Init(ASampleRate);

			var key = new WaveBufferKey(this.GetType(), ASampleRate);

			if (!FSamples.TryGetValue(key, out FSample))
			{
				var sw = Stopwatch.StartNew();
				FSample = CreateSample(ASampleRate);
				sw.Stop();
				Debug.WriteLine($"CreateSample '{key}' by {sw.ElapsedMilliseconds} ms");
				FSamples.Add(key, FSample);
			}
		}

		protected abstract double[] CreateSample(int ASampleRate);

		public override double GetValue(double ATime, double AFrequency)
		{
			return FSample[(ulong)(ATime * SampleRate * AFrequency) % (ulong)SampleRate];
		}
	}

	public class BufferSineWaveForm : BufferWaveForm
	{
		public override string Name => "BSine";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = Math.Sin(TwoPi*i/(ASampleRate - 1));
			return r;
		}
	}

	public class BufferTriangleWaveForm : BufferWaveForm
	{
		public override string Name => "Triangle";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			int f = ASampleRate / 4;
			for (int i = 0; i < ASampleRate; i++)
				switch (i / f)
				{
					case 0:
						r[i] = (double)i / f;
						break;
					case 1:
					case 2:
						r[i] = 1 - (double)(i - f) / f;
						break;
					case 3:
						r[i] = -1 + (double)(i - 3*f) / f;
						break;
				}
			return r;
		}
	}

	public class BufferSquareWaveForm : BufferWaveForm
	{
		public override string Name => "Square";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			int f = ASampleRate / 2;
			for (int i = 0; i < ASampleRate; i++)
				if (i < f)
					r[i] = 1;
				else
					r[i] = -1;
			return r;
		}
	}

	public class BufferSawRWaveForm : BufferWaveForm
	{
		public override string Name => "SawR";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = (double)i/(ASampleRate - 1);
			return r;
		}
	}

	public class BufferSawLWaveForm : BufferWaveForm
	{
		public override string Name => "SawL";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = 1- (double)i / (ASampleRate - 1);
			return r;
		}
	}

}
