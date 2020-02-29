namespace SoundMap.Temperaments
{
	public class Tone
	{
		public double Frequency { get; }
		public string Name { get; }
		public int Id { get; }

		public Tone()
		{ }

		public Tone(double frequency, int id, string name = "")
		{
			Frequency = frequency;
			Id = id;
			Name = name;
		}

		public override string ToString()
		{
			return $"{Name} ({Frequency:F3})";
		}
	}
}
