using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SoundMap.Waveforms
{
	public abstract class BufferWaveform : Waveform
	{
		protected readonly Dictionary<int, double[]> FSamples = new Dictionary<int, double[]>();
		protected double[] FSample = null;

		[XmlIgnore]
		public bool NeedInit { get; protected set; } = false;

		[XmlIgnore]
		public double[] Sample => FSample;

		/// <summary>
		/// Вызывается при запуске воспроизведения устройства
		/// </summary>
		public override void Init(int ASampleRate)
		{
			base.Init(ASampleRate);

			if (!FSamples.TryGetValue(ASampleRate, out FSample))
			{
				var sw = Stopwatch.StartNew();
				FSample = CreateSample(ASampleRate);
				sw.Stop();
				Debug.WriteLine($"CreateSample '{ASampleRate}' by {sw.ElapsedMilliseconds} ms");
				FSamples.Add(ASampleRate, FSample);
			}
			else if (NeedInit)
			{
				FSample = CreateSample(ASampleRate);
				FSamples[ASampleRate] = FSample;
				NeedInit = false;
			}
		}

		//public override Waveform Clone()
		//{
		//	var r = (BufferWaveform)base.Clone();
		//	r.NeedInit = this.NeedInit;
		//	return r;
		//}

		/// <summary>
		/// Вызывается в Init, если коллекция Samples не содержит семпла этого типа с нужным ASampleRate
		/// </summary>
		protected abstract double[] CreateSample(int ASampleRate);

		public override double GetValue(double ATime, double AFrequency)
		{
			return FSample[(ulong)(ATime * SampleRate * AFrequency) % (ulong)SampleRate];
		}
	}

	public class BufferSineWaveForm : BufferWaveform
	{
		public override string Name => "BSine";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = Math.Sin(TwoPi * i / (ASampleRate - 1));
			return r;
		}
	}

	public class BufferTriangleWaveForm : BufferWaveform
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
						r[i] = -1 + (double)(i - 3 * f) / f;
						break;
				}
			return r;
		}
	}

	public class BufferSquareWaveForm : BufferWaveform
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

	public class BufferSawRWaveForm : BufferWaveform
	{
		public override string Name => "SawR";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = (double)i / (ASampleRate - 1);
			return r;
		}
	}

	public class BufferSawLWaveForm : BufferWaveform
	{
		public override string Name => "SawL";

		protected override double[] CreateSample(int ASampleRate)
		{
			double[] r = new double[ASampleRate];
			for (int i = 0; i < ASampleRate; i++)
				r[i] = 1 - (double)i / (ASampleRate - 1);
			return r;
		}
	}
}
