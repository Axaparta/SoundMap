using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundMap.NoteWaveProviders
{
	public class MTNoteWaveProvider : NoteWaveProvider
	{
		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo);

			var count = exclusiveTo - inclusiveFrom;
			double startTime = FTime;

			Parallel.For(0, count / 2, (n) =>
			{
				var time = startTime + n * FTimeDelta;

				SoundPointValue op = new SoundPointValue();

				for (int i = 0; i < notes.Length; i++)
					op += notes[i].GetValue(time);

				var index = inclusiveFrom + 2 * n;
				buffer[index] = (float)op.Right;
				buffer[index + 1] = (float)op.Left;
			});

			FTime += count / 2 * FTimeDelta;
		}
	}
}
