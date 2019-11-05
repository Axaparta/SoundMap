using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundProject: Observable, ISampleProvider
	{
		[XmlIgnore]
		public WaveFormat WaveFormat { get; private set; }
		private double FTime = 0;

		[XmlIgnore]
		public SoundPointCollection Points { get; } = new SoundPointCollection();

		[XmlIgnore]
		public string FileName { get; set; }

		private Action<SoundPoint> FSoundControlAddPointAction = null;
		private Action<SoundPoint> FSoundControlDeletePointAction = null;

		public SoundProject()
		{
			Points.CollectionChanged += Points_CollectionChanged;
		}

		[XmlElement(ElementName="Points")]
		public SoundPoint[] SerializePoints
		{
			get => Points.ToArray();
			set
			{
				Points.Clear();
				foreach (var p in value)
					Points.Add(p);
			}
		}

		private void Points_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			//SetPoints(Points);
		}

		public void ConfigureGenerator(int ASampleRate, int AChannels)
		{
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(ASampleRate, AChannels);
			FTime = 0;
		}

		public string Title
		{
			get
			{
				if (string.IsNullOrEmpty(FileName))
					return App.AppName;
				return $"{App.AppName} [{Path.GetFileNameWithoutExtension(FileName)}]";
			}
		}


		public int Read(float[] buffer, int offset, int count)
		{
			int maxn = offset + count;
			int maxOldIndex = 60;
			int oldIndex = maxOldIndex;

			var points = Points.ToArray();

			for (int n = offset; n < maxn; n++)
			{
				var op = (float)GetValue(points, FTime);

				switch (WaveFormat.Channels)
				{
					case 1:
						buffer[n] = op;
						break;
					case 2:
						buffer[n] = op;
						n++;
						buffer[n] = op;
						break;
					default:
						throw new NotSupportedException($"Channels count: " + WaveFormat.Channels);
				}

				FTime += 1 / (double)WaveFormat.SampleRate;
			}

			return count;
		}

		public double GetValue(SoundPoint[] APoints, double ATime)
		{
			double max = 0;
			double r = 0;
			foreach (var p in APoints)
			{
				max += p.Volume;
				r += p.Volume * Math.Sin(p.Frequency * ATime * Math.PI * 2);
			}

			if (max > 1)
				r = r / max;

			return r;
		}

		public Action<SoundPoint> SoundControlAddPointAction
		{
			get
			{
				if (FSoundControlAddPointAction == null)
					FSoundControlAddPointAction = new Action<SoundPoint>((sp) => Points.AddSoundPoint(sp));
				return FSoundControlAddPointAction;
			}
		}

		public Action<SoundPoint> SoundControlDeletePointAction
		{
			get
			{
				if (FSoundControlDeletePointAction == null)
					FSoundControlDeletePointAction = new Action<SoundPoint>((sp) => Points.RemoveSoundPoint(sp));
				return FSoundControlDeletePointAction;
			}
		}

		public void SaveToFile(string AFileName)
		{
			try
			{
				XmlHelper.Save(this, AFileName);
				FileName = AFileName;
				NotifyPropertyChanged(nameof(Title));
			}
			catch (Exception ex)
			{
				App.ShowError(ex.Message);
			}
		}
	}
}
