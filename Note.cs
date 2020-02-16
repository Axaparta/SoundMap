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
		private NotePhase FToSetPhase = NotePhase.Playing;
		private NotePhase FPhase = NotePhase.Init;
		private double FStartTime = double.NaN;

		public SoundPoint[] Points { get; set; }
		public object Key { get; }
		public double Volume { get; set; } = 1;
		public AdsrEnvelope Envelope { get; }

		public Note(SoundPoint[] APoints, WaveFormat AFormat, object AKey, AdsrEnvelope AEnvelope, double AVolume = 1)
		{
			Points = APoints;
			Key = AKey;
			Envelope = AEnvelope;
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
					Envelope.Start(ATime);
					break;
				case NotePhase.Stopping:
					FToSetPhase = NotePhase.None;
					FPhase = NotePhase.Stopping;
					Envelope.Stop(ATime);
					break;
			}

			if ((FPhase == NotePhase.Stopping) && Envelope.IsDone(ATime))
				FPhase = NotePhase.Done;
		}

		public SoundPointValue GetValue(double ATime)
		{
			double max = 0;
			var t = ATime - FStartTime;
			SoundPointValue r = new SoundPointValue();
			foreach (var p in Points)
			{
				if (p.IsMute)
					continue;

				max += p.Volume;

				var v = p.Volume * p.Waveform.GetValue(t, p.Frequency);
				r += new SoundPointValue(v * p.LeftPct, v * p.RightPct);
			}

			if (max > 1)
				r /= max;

			r *= Envelope.GetValue(ATime) * Volume;

			return r;
		}
	}
}
