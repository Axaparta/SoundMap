using System.ComponentModel;
using System.Diagnostics;

namespace SoundMap.NoteWaveProviders
{
	[NoteWave("Singlethread")]
	public class STNoteWaveProvider: NoteWaveProvider
	{
		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo, NoteWaveArgs args)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo, args);

			for (int n = inclusiveFrom; n < exclusiveTo; n++)
			{
				SoundPointValue op = new SoundPointValue();
				for (int i = 0; i < notes.Length; i++)
					op += notes[i].GetValue(FTime);

				op = op * args.MasterVolume;

				if (op.Right > args.MaxR)
					args.MaxR = op.Right;

				//op = new SoundPointValue();

				buffer[n] = (float)op.Left;

				n++;

				if (op.Left > args.MaxL)
					args.MaxL = op.Left;
				buffer[n] = (float)op.Right;

				FTime += FTimeDelta;
			}
		}
	}
}
