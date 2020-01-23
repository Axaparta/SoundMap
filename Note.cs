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
		private readonly AdsrEnvelope FEnvelope;
		private NotePhase FToSetPhase = NotePhase.Playing;
		private NotePhase FPhase = NotePhase.Init;
		private double FStartTime = double.NaN;

		public SoundPoint[] Points { get; set; }
		public object Key { get; }

		public Note(SoundPoint[] APoints, WaveFormat AFormat, object AKey, AdsrEnvelope AEnvelope)
		{
			Points = APoints;
			Key = AKey;
			FEnvelope = AEnvelope;
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
					FEnvelope.Start(ATime);
					break;
				case NotePhase.Stopping:
					FToSetPhase = NotePhase.None;
					FPhase = NotePhase.Stopping;
					FEnvelope.Stop(ATime);
					break;
			}

			if ((FPhase == NotePhase.Stopping) && FEnvelope.IsDone(ATime))
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
				var v = p.Volume * p.GetValue(t);

				if (p.IsSolo)
					return v;

				r += v;
			}

			if (max > 1)
				r /= max;

			r *= FEnvelope.GetValue(ATime);

			return r;
		}
	}
}
