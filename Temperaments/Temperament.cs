using System.Linq;

namespace SoundMap.Temperaments
{
	public enum Interval
	{
		Tone,
		HalfTone
	}

	public enum TemperamentMode
	{ 
		Major,
		Minor
	}

	public abstract class Temperament
	{
		public string Name { get; }
		public double BaseFrequency { get; }

		protected Temperament(string name, double baseFrequency)
		{
			BaseFrequency = baseFrequency;
			Name = name;
		}

		/// <summary>
		/// Выдаёт все тоны указанного строя, смещение относительно основного тона
		/// </summary>
		public abstract Tone[] GetTemperamentTones(int fromOffset, int toOffset);

		public Tone GetTemperamentTone(int offset) => GetTemperamentTones(offset, offset).First();

		/// <summary>
		/// Выдаёт тон или null, соответствующий нажатой клавише,
		/// Базовая частота будем смещена.
		/// </summary>
		/// <param name="halfToneOffset">Смещение ноты относительно ДО в первой октаве</param>
		/// <returns></returns>
		public virtual Tone GetKeyboardTone(int halfToneOffset)
		{
			return null;
		}
	}
}
