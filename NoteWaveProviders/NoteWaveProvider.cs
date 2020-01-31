using NAudio.Wave;
using System.Diagnostics;

namespace SoundMap.NoteWaveProviders
{
	public abstract class NoteWaveProvider
	{
		protected double FTime = 0;
		protected double FTimeDelta;
		public WaveFormat Format { get; private set; }

		public virtual void Init(WaveFormat AFormat)
		{
			FTime = 0;
			FTimeDelta = 1 / (double)AFormat.SampleRate;
			Format = AFormat;
			Debug.Assert(AFormat.Channels == 2, $"NoteWaveProvider.Init: AFormat.Channels == {AFormat.Channels}");
		}

		public virtual void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo)
		{
			Debug.Assert(notes != null, "NoteWaveProvider.Read: notes == null");
			foreach (var n in notes)
				n.UpdatePhase(FTime);
		}
	}
}
