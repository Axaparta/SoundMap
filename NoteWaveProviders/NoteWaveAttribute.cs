using System;

namespace SoundMap.NoteWaveProviders
{
	[AttributeUsage(AttributeTargets.Class)]
	public class NoteWaveAttribute: Attribute
	{
		public string Name { get; }
		public bool IsOpenCLEnabled { get; }

		public NoteWaveAttribute(string name, bool openCLEnabled = false)
		{
			Name = name;
			IsOpenCLEnabled = openCLEnabled;
		}
	}
}
