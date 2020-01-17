using NAudio.Wave;
using SoundMap.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundProject: Observable, ISampleProvider
	{
		public static readonly string FileFilter = "SoundMap project (*.smp)|*.smp";

		private double FTime = 0;
		private ProjectSettings FSettings = new ProjectSettings();
		private NoteSourceEnum FNoteSource = NoteSourceEnum.None;
		private RelayCommand FNotePanicCommand = null;

		private readonly List<Note> FNotes = new List<Note>();
		private readonly object FNotesLock = new object();
		private Note FContinueOneNote = null;
		private string FStatus;

		[XmlIgnore]
		public WaveFormat WaveFormat { get; private set; }

		[XmlIgnore]
		public SoundPointCollection Points { get; } = new SoundPointCollection();

		[XmlIgnore]
		public SoundPointCollection SelectedPoints { get; } = new SoundPointCollection();

		[XmlIgnore]
		public string FileName { get; set; }

		[XmlIgnore]
		public bool DebugMode { get; set; } = false;
		private readonly Stopwatch FDebugStopwatch = new Stopwatch();
		private long FOldStartRead = 0;

		public NoteSourceEnum NoteSource
		{
			get => FNoteSource;
			set
			{
				if (FNoteSource != value)
				{
					FNoteSource = value;
					NotifyPropertyChanged(nameof(NoteSource));
				}
			}
		}

		public void NotePanic()
		{
			lock (FNotesLock)
				FNotes.Clear();
		}

		public ProjectSettings Settings
		{
			get
			{
				if (FSettings == null)
					FSettings = new ProjectSettings();
				return FSettings;
			}
			set
			{
				FSettings = value;
				NotifyPropertyChanged(nameof(Settings));
			}
		}

		private SoundPointEvent FSoundControlAddPointAction = null;
		private SoundPoint FSelectedPoint = null;

		public SoundProject()
		{
			Points.CollectionChanged += Points_CollectionChanged;
			Points.PointPropertyChanged += Points_PointPropertyChanged;
			SelectedPoints.CollectionChanged += SelectedPoints_CollectionChanged;

			FDebugStopwatch.Start();
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
			FContinueOneNote = new Note(null, WaveFormat, null);
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
			long startRead = FDebugStopwatch.ElapsedMilliseconds;

			int maxn = offset + count;

			Note[] notes = null;

			switch (FNoteSource)
			{
				//case NoteSourceEnum.None:
				//case NoteSourceEnum.MIDI:
				//	notes = new Note[0];
				//	break;
				case NoteSourceEnum.ContinueOne:
					FContinueOneNote.Points = Points.ToArray();
					notes = new Note[] { FContinueOneNote };
					break;
				case NoteSourceEnum.Keyboard:
					lock (FNotesLock)
					{
						List<Note> toDeleteNotes = new List<Note>();
						foreach (var n in FNotes)
							if (n.Phase == NotePhase.Done)
								toDeleteNotes.Add(n);
						foreach (var n in toDeleteNotes)
							FNotes.Remove(n);
						notes = FNotes.ToArray();
					}
					break;
			}

			if (notes == null)
				return count;

			foreach (var n in notes)
				n.UpdatePhase(FTime);

#if false
			for (int n = offset; n < maxn; n++)
			{
				SoundPointValue op = new SoundPointValue();
				if (notes == null)
					op = GetValue(points, FTime);
				else
				{
					for (int i = 0; i < notes.Length; i++)
						op += notes[i].GetValue(FTime);
				}

				switch (WaveFormat.Channels)
				{
					case 1:
						buffer[n] = (float)op.Right;
						break;
					case 2:
						buffer[n] = (float)op.Right;
						n++;
						buffer[n] = (float)op.Left;
						break;
					default:
						throw new NotSupportedException($"Channels count: " + WaveFormat.Channels);
				}

				FTime += 1 / (double)WaveFormat.SampleRate;
			}
#else
			double startTime = FTime;

			switch (WaveFormat.Channels)
			{
				case 2:
					Parallel.For(0, count/2, (n) =>
					{
						var time = startTime + n / (double)WaveFormat.SampleRate;

						SoundPointValue op = new SoundPointValue();

						for (int i = 0; i < notes.Length; i++)
							op += notes[i].GetValue(time);

						var index = offset + 2 * n;
						buffer[index] = (float)op.Right;
						buffer[index + 1] = (float)op.Left;
					});

					FTime += (count / 2) / (double)WaveFormat.SampleRate;
					break;
				default:
					throw new NotSupportedException($"Channels count: " + WaveFormat.Channels);
			}
#endif

			long endRead = FDebugStopwatch.ElapsedMilliseconds;
			var dotsPerChannel = count / WaveFormat.Channels;
			var bufferTime = 1000 * dotsPerChannel / WaveFormat.SampleRate;

			if (endRead - startRead >= bufferTime)
				Debug.WriteLine("Overload! " + (endRead - startRead - bufferTime));

			if (DebugMode)
			{
				Debug.WriteLine($"NewStart - OldStart = {startRead - FOldStartRead}, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
				Debug.WriteLine($"Start {startRead}, end {endRead}, D: {endRead - startRead}, Count: {count}, CTime (msec): {bufferTime}, (Saple/Sec): {WaveFormat.SampleRate}");
			}

			Interlocked.Exchange(ref FStatus, $"Load: {100*(endRead - startRead)/ bufferTime, 3}% ({bufferTime})");

			FOldStartRead = startRead;
			return count;
		}

		public SoundPointValue GetValue(SoundPoint[] APoints, double ATime)
		{
			double max = 0;
			SoundPointValue r = new SoundPointValue();

#if true

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
				r /= max;

#else

			r.Left = Math.Sin(2 * 3.14 * 440 * ATime);
			r.Right = r.Left;

#endif

			if (Math.Abs(r.Left) > 1)
				Debug.WriteLine("");

			return r;
		}

		public SoundPointEvent SoundControlAddPointAction
		{
			get
			{
				if (FSoundControlAddPointAction == null)
					FSoundControlAddPointAction = new SoundPointEvent((sp) =>
					{
						//sp.Kind = NewPointKind;
						Points.Add(sp);
					});
				return FSoundControlAddPointAction;
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

		public void AddNote(object AKey, double AMultipler)
		{
			var a = Points.Select(p =>
			{
				p = p.Clone();
				p.Frequency *= AMultipler;
				p.IsNote = true;
				return p;
			}).ToArray();

			lock (FNotesLock)
			{
				FNotes.Add(new Note(a, WaveFormat, AKey));
			}
		}

		public void AddNoteByHalftone(object AKey, int AHalftoneOffset)
		{
			var v = Math.Pow(2, (double)AHalftoneOffset / 12);
			AddNote(AKey, v);
		}

		public void DeleteNote(object AKey)
		{
			lock (FNotesLock)
				foreach (var n in FNotes)
					if ((n.Key.Equals(AKey)) && (n.Phase == NotePhase.Playing))
					{
						n.Phase = NotePhase.Stopping;
						break;
					}
		}

		public string Status
		{
			get
			{
				return FStatus;
			}
		}

		public RelayCommand NotePanicCommand
		{
			get
			{
				if (FNotePanicCommand == null)
					FNotePanicCommand = new RelayCommand((obj) => NotePanic());
				return FNotePanicCommand;
			}
		}
	}
}
