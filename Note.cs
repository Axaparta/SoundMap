using NAudio.Dsp;
using NAudio.Wave;

namespace SoundMap
{
	public enum NotePhase
	{
		None,
		/// <summary>Фаза после создания ноты. Перед первым заполнением буфера переходит на Playing</summary>
		Init,
		/// <summary></summary>
		Playing,
		Stopping,
		Done
	}

	public class Note
	{
		public SoundPoint[] Points { get; }
		private readonly EnvelopeGenerator FEnvelope;
		private NotePhase FToSetPhase = NotePhase.Playing;
		private NotePhase FPhase = NotePhase.Init;

		private double FStartTime = double.NaN;
		private double FStopTime = double.NaN;

		public Note(SoundPoint[] APoints, WaveFormat AFormat)
		{
			Points = APoints;

			//https://github.com/naudio/NAudio/blob/master/NAudio/Wave/SampleProviders/AdsrSampleProvider.cs
			FEnvelope = new EnvelopeGenerator();
			FEnvelope.AttackRate = 0.01f * AFormat.SampleRate;
			FEnvelope.DecayRate = 0.8f * AFormat.SampleRate;
			FEnvelope.SustainLevel = 0.0f;
			FEnvelope.ReleaseRate = 0.1f * AFormat.SampleRate;

			FEnvelope.Gate(true);
		}

		public NotePhase Phase
		{
			get => FPhase;
			set => FToSetPhase = value;
		}

		public void UpdatePhase(double ATime)
		{
			switch (FToSetPhase)
			{
				case NotePhase.Playing:
					FToSetPhase = NotePhase.None;
					FPhase = NotePhase.Playing;
					FStartTime = ATime;
					break;
				case NotePhase.Stopping:
					FToSetPhase = NotePhase.None;
					FPhase = NotePhase.Stopping;
					FStopTime = ATime;
					break;
			}

			if ((FPhase == NotePhase.Stopping) && (ATime - FStopTime > 1))
				FPhase = NotePhase.Done;
		}

		public SoundPointValue GetValue(double ATime)
		{
			double max = 0;
			SoundPointValue r = new SoundPointValue();
			foreach (var p in Points)
			{
				if (p.IsMute)
					continue;

				max += p.Volume;
				var v = p.Volume * p.GetValue(ATime);

				if (p.IsSolo)
					return v;

				r += v;
			}

			if (max > 1)
				r /= max;

			if (FEnvelope.State == EnvelopeGenerator.EnvelopeState.Idle)
				return new SoundPointValue();

			//r *= FEnvelope.Process();

			return r;
		}
	}
}
