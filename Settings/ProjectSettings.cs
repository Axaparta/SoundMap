using System;

namespace SoundMap.Settings
{
	[Serializable]
	public class ProjectSettings: Observable, ICloneable
	{
		private double FMinFrequency = 50;
		private double FMaxFrequency = 2000;

		public ProjectSettings()
		{ }

		public double MinFrequency
		{
			get => FMinFrequency;
			set
			{
				if (FMinFrequency != value)
				{
					FMinFrequency = value;
					NotifyPropertyChanged(nameof(MinFrequency));
				}
			}
		}

		public double MaxFrequency
		{
			get => FMaxFrequency;
			set
			{
				if (FMaxFrequency != value)
				{
					FMaxFrequency = value;
					NotifyPropertyChanged(nameof(MaxFrequency));
				}
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ProjectSettings Clone()
		{
			ProjectSettings ps = (ProjectSettings)MemberwiseClone();
			return  ps;
		}
	}
}
