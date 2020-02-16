using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundMap.NoteWaveProviders
{
	[NoteWave("Multithread")]
	public class MTNoteWaveProvider : NoteWaveProvider
	{
		public override void Read(Note[] notes, float[] buffer, int inclusiveFrom, int exclusiveTo, NoteWaveArgs args)
		{
			base.Read(notes, buffer, inclusiveFrom, exclusiveTo, args);

			var count = exclusiveTo - inclusiveFrom;
			var count2 = count / 2;
			double startTime = FTime;

			Parallel.For(0, count2, (n) =>
			{
				var time = startTime + n * FTimeDelta;

				SoundPointValue op = new SoundPointValue();

				for (int i = 0; i < notes.Length; i++)
					op += notes[i].GetValue(time);

				op *= args.MasterVolume;

				var index = inclusiveFrom + 2 * n;
				buffer[index] = (float)op.Left;
				buffer[index + 1] = (float)op.Right;
			});

			unsafe
			{
				// Фиксирую указатель на массив
				fixed (float* bf = buffer)
				{
					// Указатель на начало массива
					float* f = bf;
					// Количество итераций - половина размера массива
					var c = count2;
					while (c > 0)
					{
						// Проверка для правого канала
						if (*f > args.MaxR)
							args.MaxR = *f;
						f++;
						// Проверка для левого канала
						if (*f > args.MaxL)
							args.MaxL = *f;
						f++;
						c--;
					}
				}
			}

			FTime += count2 * FTimeDelta;
		}
	}
}
