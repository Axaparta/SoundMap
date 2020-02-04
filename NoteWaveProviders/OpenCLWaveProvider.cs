using Cloo;
using Cloo.Bindings;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SoundMap.NoteWaveProviders
{
	[NoteWave("OpenCL", true)]
	public class OpenCLWaveProvider : NoteWaveProvider, IDisposable
	{
		private ComputeDevice FDevice = null;
		private ComputeContext FContext = null;

		private ComputeProgram program = null;
		private ComputeKernel kernel = null;
		private ComputeCommandQueue commands = null;
		private readonly string FProgramSource;

		private Stopwatch sw = Stopwatch.StartNew();

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

		public static void BN(CLProgramHandle programHandle, IntPtr notifyDataPtr)
		{

		}

		public override void Init(WaveFormat AFormat)
		{
			base.Init(AFormat);

			try
			{
				FDevice = App.Settings.Preferences.OpenCL.GetComputeDevice();
				FContext = new ComputeContext(new ComputeDevice[] { FDevice }, new ComputeContextPropertyList(FDevice.Platform), null, IntPtr.Zero);

				program = new ComputeProgram(FContext, FProgramSource);

				program.Build(new[] { FDevice }, null, null, IntPtr.Zero);
				
				kernel = program.CreateKernel("Wave");
				commands = new ComputeCommandQueue(FContext, FContext.Devices[0], ComputeCommandQueueFlags.None);
			}
			catch (BuildProgramFailureComputeException bex)
			{
				Debug.WriteLine(bex.Message);
				Debug.WriteLine(program.GetBuildLog(FDevice));
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
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

		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo, double masterVolume)
		{
			var s = sw.ElapsedMilliseconds;

			base.Read(notes, buffer, inclusiveFrom, exclusiveTo, masterVolume);
			int timeChannelsCount = exclusiveTo - inclusiveFrom;

			int nCount = notes.Length;
			int pCount = 0;
			if (nCount > 0)
				pCount = notes[0].Points.Length;

			float[] args = new float[] { (float)FTime, (float)FTimeDelta, inclusiveFrom, nCount, pCount, (float)masterVolume };
			float[] ampl = new float[nCount * pCount];
			float[] freq = new float[nCount * pCount];
			float[] envs = new float[nCount * AdsrEnvelope.CLParamSize];
			float[] result = new float[timeChannelsCount];

			int index = 0;
			for (int n = 0; n < nCount; n++)
			{
				for (int p = 0; p < pCount; p++, index++)
				{
					ampl[index] = (float)notes[n].Points[p].Volume;
					freq[index] = (float)notes[n].Points[p].Frequency;
				}
				var env = notes[n].Envelope.CLParams;
				Array.Copy(env, 0, envs, n * AdsrEnvelope.CLParamSize, AdsrEnvelope.CLParamSize);
			}

			long br = 0, ar = 0, ac = 0;

			if (nCount * pCount > 0)
			{
				br = sw.ElapsedMilliseconds;

				Run(args, ampl, freq, envs, result);

				ar = sw.ElapsedMilliseconds;

				if (App.DebugMode)
					Debug.WriteLine("");

				for (int i = 0, j = inclusiveFrom; i < result.Length; i++, j++)
					buffer[j] = result[i];

				ac = sw.ElapsedMilliseconds;
			}
			else
				for (int i = 0, j = inclusiveFrom; i < result.Length; i++, j++)
					buffer[j] = 0;

			FTime += timeChannelsCount / 2 * FTimeDelta;

			var gc = sw.ElapsedMilliseconds;

			if (App.DebugMode)
				Debug.WriteLine($"OpenCLWaveProvider.Read: tot {gc - s}");
		}

		private void Run(float[] args, float[] ampl, float[] freq, float[] envs, float[] result)
		{
			//var s = sw.ElapsedMilliseconds;

			ComputeBuffer<float> argsBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, args);
			ComputeBuffer<float> amplBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, ampl);
			ComputeBuffer<float> freqBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, freq);
			ComputeBuffer<float> envsBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, envs);
			ComputeBuffer<float> resultBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.WriteOnly, result.Length);

			kernel.SetMemoryArgument(0, argsBuffer);
			kernel.SetMemoryArgument(1, amplBuffer);
			kernel.SetMemoryArgument(2, freqBuffer);
			kernel.SetMemoryArgument(3, envsBuffer);
			kernel.SetMemoryArgument(4, resultBuffer);

			//var f = sw.ElapsedMilliseconds;

			commands.Execute(kernel, null, new long[] { result.Length / 2 }, null, null);

			//var e = sw.ElapsedMilliseconds;

			commands.ReadFromBuffer(resultBuffer, ref result, true, null);

			//var r = sw.ElapsedMilliseconds;

			commands.Finish();

			argsBuffer.Dispose();
			amplBuffer.Dispose();
			freqBuffer.Dispose();
			envsBuffer.Dispose();
			resultBuffer.Dispose();

			//var d = sw.ElapsedMilliseconds;

			//if (App.DebugMode)
			//	Debug.WriteLine($"OpenCLWaveProvider.Run: tot: {d - s}");

		}
	}
}
