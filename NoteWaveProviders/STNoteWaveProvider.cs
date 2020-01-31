using System.Diagnostics;

namespace SoundMap.NoteWaveProviders
{
	public class STNoteWaveProvider: NoteWaveProvider
	{
		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo);

			for (int n = inclusiveFrom; n < exclusiveTo; n++)
			{
				SoundPointValue op = new SoundPointValue();
				for (int i = 0; i < notes.Length; i++)
					op += notes[i].GetValue(FTime);

				buffer[n] = (float)op.Right;
				n++;
				buffer[n] = (float)op.Left;

				FTime += FTimeDelta;
			}
		}
	}
}
