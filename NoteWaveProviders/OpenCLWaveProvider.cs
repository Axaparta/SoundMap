using Cloo;
using Cloo.Bindings;
using NAudio.Wave;
using SoundMap.Waveforms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SoundMap.NoteWaveProviders
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct OpenCLPointInfo
	{
		public float ampl;
		public float freq;
		public float leftPct;
		public float rightPct;
		public int wfIndex;
		public int isMute;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct OpenCLInfo
	{
		public float startTime;
		public float timeDelta;
		public int sampleRate;
		public float masterVolume;
		public uint inclusiveFrom;

		public uint noteCount;
		// Points per note
		public uint pointCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct OpenCLEnvelope
	{
		public float attakK;
		public float attacTime;

		public float decayK;
		public float decayTime;

		public float releaseK;
		public float releaseTime;

		public float startTime;
		public float stopTime;
		public float stopValue;
		public float sustainLevel;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct OpenCLNote
	{
		public OpenCLEnvelope envelope;
		public float volume;
	}

	[NoteWave("OpenCL", true)]
	public class OpenCLWaveProvider : NoteWaveProvider, IDisposable
	{
		private ComputeDevice FDevice = null;
		private ComputeContext FContext = null;

		private ComputeProgram program = null;
		private ComputeKernel kernel = null;
		private ComputeCommandQueue commands = null;
		private readonly string FProgramSource;
		private int FOldSamplesHash = 0;

		ComputeBuffer<float> FWaveformBuffer = null;

		private Stopwatch sw = Stopwatch.StartNew();

		public OpenCLWaveProvider()
		{
			using (var s = Assembly.GetAssembly(typeof(OpenCLWaveProvider)).GetManifestResourceStream("SoundMap.NoteWaveProviders.WaveProgram.cl"))
			using (var tr = new StreamReader(s, Encoding.UTF8))
				FProgramSource = tr.ReadToEnd();
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
			FWaveformBuffer.Dispose();

			commands.Dispose();
			kernel.Dispose();
			program.Dispose();
			FContext.Dispose();
			FDevice = null;
		}

		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo, NoteWaveArgs args)
		{
			var s = sw.ElapsedMilliseconds;

			if ((FOldSamplesHash != args.SamplesHash) || (FOldSamplesHash == 0))
			{
				FOldSamplesHash = args.SamplesHash;
				InitSampleArgs(args.Samples);
			}

			Dictionary<int, int> wfIndexDict = new Dictionary<int, int>();
			for (int i = 0; i < args.Samples.Length; i++)
				wfIndexDict.Add(args.Samples[i].Key.GetHashCode(), i);

			base.Read(notes, buffer, inclusiveFrom, exclusiveTo, args);
			int timeChannelsCount = exclusiveTo - inclusiveFrom;

			int nCount = notes.Length;
			int pCount = 0;
			if (nCount > 0)
				pCount = notes[0].Points.Length;

			OpenCLInfo info = new OpenCLInfo()
			{
				startTime = (float)FTime,
				timeDelta = (float)FTimeDelta,
				sampleRate = Format.SampleRate,
				noteCount = (uint)nCount,
				pointCount = (uint)pCount,
				masterVolume = (float)args.MasterVolume
			};

			info.inclusiveFrom = (uint)inclusiveFrom;

			OpenCLPointInfo[] points = new OpenCLPointInfo[nCount * pCount];

			OpenCLNote[] envs = new OpenCLNote[nCount];
			float[] result = new float[timeChannelsCount];

			int index = 0;
			for (int n = 0; n < nCount; n++)
			{
				for (int p = 0; p < pCount; p++, index++)
				{
					points[index] = new OpenCLPointInfo()
					{
						ampl = (float)notes[n].Points[p].Volume,
						freq = (float)notes[n].Points[p].Frequency,
						leftPct = (float)notes[n].Points[p].LeftPct,
						rightPct = (float)notes[n].Points[p].RightPct,
						wfIndex = -1,
						isMute = (byte)(notes[n].Points[p].IsMute ? 1 : 0)
					};
					if (wfIndexDict.TryGetValue(notes[n].Points[p].Waveform.GetHashCode(), out var wfi))
						points[index].wfIndex = wfi;
				}
				envs[n] = new OpenCLNote()
				{
					envelope = notes[n].Envelope.GetOpenCL(),
					volume = (float)notes[n].Volume
				};
			}

			long br = 0, ar = 0, ac = 0;

			if (nCount * pCount > 0)
			{
				br = sw.ElapsedMilliseconds;

				Run(new OpenCLInfo[] { info }, points, envs, result);

				ar = sw.ElapsedMilliseconds;

				if (App.DebugMode)
					Debug.WriteLine("");

				for (int i = 0, j = inclusiveFrom; i < result.Length; i++, j++)
				{
					var fR = result[i];
					if (fR > args.MaxR)
						args.MaxR = fR;
					buffer[j] = fR;

					i++; j++;

					var fL = result[i];
					if (fL > args.MaxL)
						args.MaxL = fL;
					buffer[j] = fL;
				}
					

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

		private void InitSampleArgs(KeyValuePair<int, double[]>[] samples)
		{
			if (FWaveformBuffer != null)
				FWaveformBuffer.Dispose();

			List<float> s = new List<float>(samples.Length * Format.SampleRate);

			// double array to float array
			foreach (var si in samples)
			{
				float[] buf = new float[si.Value.Length];
				for (int i = 0; i < si.Value.Length; i++)
					buf[i] = (float)si.Value[i];
				s.AddRange(buf);
			}

			// array can't be empty
			if (s.Count == 0)
				s.Add(0);

			FWaveformBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, s.ToArray());

			kernel.SetMemoryArgument(2, FWaveformBuffer);
		}

		private void Run(OpenCLInfo[] info, OpenCLPointInfo[] points, OpenCLNote[] envs, float[] result)
		{
			//var s = sw.ElapsedMilliseconds;

			ComputeBuffer<OpenCLInfo> infoBuffer = new ComputeBuffer<OpenCLInfo>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, info);
			ComputeBuffer<OpenCLPointInfo> pointsBuffer = new ComputeBuffer<OpenCLPointInfo>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, points);

			ComputeBuffer<OpenCLNote> envsBuffer = new ComputeBuffer<OpenCLNote>(FContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, envs);
			ComputeBuffer<float> resultBuffer = new ComputeBuffer<float>(FContext, ComputeMemoryFlags.WriteOnly, result.Length);

			kernel.SetMemoryArgument(0, infoBuffer);
			kernel.SetMemoryArgument(1, pointsBuffer);
			// 2 - FWaveformBuffer
			kernel.SetMemoryArgument(3, envsBuffer);
			kernel.SetMemoryArgument(4, resultBuffer);

			//var f = sw.ElapsedMilliseconds;

			commands.Execute(kernel, null, new long[] { result.Length / 2 }, null, null);

			//var e = sw.ElapsedMilliseconds;

			commands.ReadFromBuffer(resultBuffer, ref result, true, null);

			//var r = sw.ElapsedMilliseconds;

			commands.Finish();

			infoBuffer.Dispose();
			pointsBuffer.Dispose();
			envsBuffer.Dispose();
			resultBuffer.Dispose();

			//var d = sw.ElapsedMilliseconds;

			//if (App.DebugMode)
			//	Debug.WriteLine($"OpenCLWaveProvider.Run: tot: {d - s}");

		}
	}
}
