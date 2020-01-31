using Cloo;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SoundMap.NoteWaveProviders
{
	public class OpenCLWaveProvider : NoteWaveProvider, IDisposable
	{
		private ComputeDevice FDevice = null;
		private ComputeContext FContext = null;

		private ComputeProgram program = null;
		private ComputeKernel kernel = null;
		private ComputeCommandQueue commands = null;
		private readonly string FProgramSource;
		//private ComputeEventList eventList = new ComputeEventList();

		//static void PrintManifestResourceNames()
		//{
		//	var exass = Assembly.GetExecutingAssembly();
		//	//var exass = typeof(GLCL.Drawing2D.Program2D).Assembly;

		//	Console.WriteLine("Buildin resources:");
		//	foreach (var s in exass.GetManifestResourceNames())
		//		Console.WriteLine("  " + s);

		//	Console.WriteLine();
		//}

		public OpenCLWaveProvider()
		{
			using (var s = Assembly.GetAssembly(typeof(OpenCLWaveProvider)).GetManifestResourceStream("SoundMap.NoteWaveProviders.WaveProgram.cl"))
			using (var tr = new StreamReader(s, Encoding.UTF8))
				FProgramSource = tr.ReadToEnd();
		}

		public override void Init(WaveFormat AFormat)
		{
			base.Init(AFormat);

			FDevice = App.Settings.Preferences.OpenCL.GetComputeDevice();
			FContext = new ComputeContext(new ComputeDevice[] { FDevice }, new ComputeContextPropertyList(FDevice.Platform), null, IntPtr.Zero);

			program = new ComputeProgram(FContext, FProgramSource);
			program.Build(null, null, null, IntPtr.Zero);

			kernel = program.CreateKernel("Wave");
			commands = new ComputeCommandQueue(FContext, FContext.Devices[0], ComputeCommandQueueFlags.None);
		}

		public void Dispose()
		{
			//foreach (var e in eventList)
			//	e.Dispose();
			//eventList.Clear();

			commands.Dispose();
			kernel.Dispose();
			program.Dispose();
			FContext.Dispose();
			FDevice = null;
		}

		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo);

			int timeChannelsCount = exclusiveTo - inclusiveFrom;

			var n = notes.FirstOrDefault();
			int nCount = 0;
			if (n != null)
				nCount = n.Points.Length;

			//if (nCount > 0)
			//	Debug.WriteLine("Before WP.Read");

			float[] args = new float[] { (float)FTime, (float)FTimeDelta, inclusiveFrom, nCount };
			float[] ampl = new float[nCount];
			float[] freq = new float[nCount];
			float[] result = new float[timeChannelsCount];

			for (int i = 0; i < nCount; i++)
			{
				ampl[i] = (float)n.Points[i].Volume;
				freq[i] = (float)n.Points[i].Frequency;
			}

			if (nCount > 0)
			{
				Run(args, ampl, freq, result);

				for (int i = 0, j = inclusiveFrom; i < result.Length; i++, j++)
					buffer[j] = result[i];
			}
			else
				for (int i = 0, j = inclusiveFrom; i < result.Length; i++, j++)
					buffer[j] = 0;

			FTime += timeChannelsCount / 2 * FTimeDelta;

			GC.Collect();
			//if (nCount > 0)
			//	Debug.WriteLine("After WP.Read");
		}

		private void Run(float[] args, float[] ampl, float[] freq, float[] result)
		{
			try
			{
				ComputeBuffer<float> argsBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, args);
				ComputeBuffer<float> amplBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, ampl);
				ComputeBuffer<float> freqBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, freq);
				ComputeBuffer<float> resultBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.WriteOnly, result.Length);

				kernel.SetMemoryArgument(0, argsBuffer);
				kernel.SetMemoryArgument(1, amplBuffer);
				kernel.SetMemoryArgument(2, freqBuffer);
				kernel.SetMemoryArgument(3, resultBuffer);

				commands.Execute(kernel, null, new long[] { result.Length / 2 }, null, null);
				commands.ReadFromBuffer(resultBuffer, ref result, true, null);

				commands.Finish();

				argsBuffer.Dispose();
				amplBuffer.Dispose();
				freqBuffer.Dispose();
				resultBuffer.Dispose();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
}
