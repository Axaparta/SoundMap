using System.ComponentModel;
using System.Diagnostics;

namespace SoundMap.NoteWaveProviders
{
	[NoteWave("Singlethread")]
	public class STNoteWaveProvider: NoteWaveProvider
	{
		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo, double masterVolume)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo, masterVolume);

			for (int n = inclusiveFrom; n < exclusiveTo; n++)
			{
				SoundPointValue op = new SoundPointValue();
				for (int i = 0; i < notes.Length; i++)
					op += notes[i].GetValue(FTime);

				op = op * masterVolume;

				buffer[n] = (float)op.Right;
				n++;
				buffer[n] = (float)op.Left;

				FTime += FTimeDelta;
			}
		}
	}
}
