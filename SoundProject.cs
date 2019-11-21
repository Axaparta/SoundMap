using NAudio.Wave;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundProject: Observable, ISampleProvider
	{
		public static readonly string FileFilter = "SoundMap project (*.smp)|*.smp";

		[XmlIgnore]
		public WaveFormat WaveFormat { get; private set; }
		private double FTime = 0;
		private double FMinFrequency = 50;
		private double FMaxFrequency = 2000;

		[XmlIgnore]
		public SoundPointCollection Points { get; } = new SoundPointCollection();

		[XmlIgnore]
		public SoundPointCollection SelectedPoints { get; } = new SoundPointCollection();

		[XmlIgnore]
		public string FileName { get; set; }

		public double MinFrequency
		{
			get => FMinFrequency;
			set
			{
				if (FMinFrequency != value)
				{
					FMinFrequency = value;
					Points_PointPropertyChanged(null, null);
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
					Points_PointPropertyChanged(null, null);
				}
			}
		}

		//Подумать как передавать эти числа точке

		[XmlIgnore]
		public PointKind NewPointKind { get; set; } = PointKind.Static;

		private Action<SoundPoint> FSoundControlAddPointAction = null;
		private Action<SoundPoint> FSoundControlDeletePointAction = null;
		private SoundPoint FSelectedPoint = null;

		public SoundProject()
		{
			Points.CollectionChanged += Points_CollectionChanged;
			Points.PointPropertyChanged += Points_PointPropertyChanged;
			SelectedPoints.CollectionChanged += SelectedPoints_CollectionChanged;
		}

		public static SoundProject CreateFromFile(string AFileName)
		{
			var r = XmlHelper.Load<SoundProject>(AFileName);
			r.FileName = AFileName;
			return r;
		}

		[XmlElement(ElementName="Points")]
		public SoundPoint[] SerializePoints
		{
			get => Points.ToArray();
			set
			{
				Points.ChangedLock();
				Points.Clear();
				if (value != null)
					foreach (var p in value)
						Points.Add(p);
				Points.ChangedUnlock();
			}
		}

		private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{

		}

		private void Points_PointPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if ((sender != null) && (e != null))
			{
				var sp = (SoundPoint)sender;
				// Устанавливается соло, но оно может быть только одно!
				if ((e.PropertyName == nameof(SoundPoint.IsSolo)) && sp.IsSolo)
				{
					Points.ChangedLock();
					foreach (var p in Points)
						p.IsSolo = p == sp;
					Points.ChangedUnlock();
				}
			}

			SelectedPoints.ChangedLock();
			SelectedPoints.Clear();
			foreach (var p in Points)
				if (p.IsSelected)
					SelectedPoints.Add(p);
			SelectedPoints.ChangedUnlock();
		}

		private void SelectedPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (SelectedPoints.Count == 1)
				SelectedPoint = SelectedPoints.First();
			else
				SelectedPoint = null;
		}

		public SoundPoint SelectedPoint
		{
			get => FSelectedPoint;
			set
			{
				if (FSelectedPoint != value)
				{
					FSelectedPoint = value;
					NotifyPropertyChanged(nameof(SelectedPoint));
				}
			}
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
				if (p.IsMute)
					continue;

				max += p.Volume;
				var v = p.Volume * p.GetValue(ATime);

				if (p.IsSolo)
					return v;

				r += v;
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
					FSoundControlAddPointAction = new Action<SoundPoint>((sp) =>
					{
						sp.Kind = NewPointKind;
						Points.Add(sp);
					});
				return FSoundControlAddPointAction;
			}
		}

		public Action<SoundPoint> SoundControlDeletePointAction
		{
			get
			{
				if (FSoundControlDeletePointAction == null)
					FSoundControlDeletePointAction = new Action<SoundPoint>((sp) => Points.Remove(sp));
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

		public void SaveSampleToFile(string AFileName)
		{

			try
			{
				using (WaveFileWriter writer = new WaveFileWriter(AFileName, WaveFormat))
				{
					float[] buf = new float[WaveFormat.AverageBytesPerSecond];
					int offset = 0;
					var rb = Read(buf, offset, buf.Length);
					writer.WriteSamples(buf, offset, rb);
				}
			}
			catch (Exception ex)
			{
				App.ShowError(ex.Message);
			}
		}
	}
}
