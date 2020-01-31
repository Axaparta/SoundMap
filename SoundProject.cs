using NAudio.Wave;
using SoundMap.NoteWaveProviders;
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
		private static Waveform[] FBuildinWaveforms = null;

		private ProjectSettings FSettings = new ProjectSettings();
		private NoteSourceEnum FNoteSource = NoteSourceEnum.ContinueOne;
		private RelayCommand FNotePanicCommand = null;

		private WaveFileWriter FFileWriter = null;
		private readonly object FFileWriterLock = new object();

		private readonly List<Note> FNotes = new List<Note>();
		private readonly object FNotesLock = new object();
		private Note FContinueOneNote = null;
		private string FStatus;
		private AdsrEnvelope FEnvelope = null;
		private NoteWaveProvider FWaveProvider = null;

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
		private bool FIsModify = false;

		public NoteSourceEnum NoteSource
		{
			get => FNoteSource;
			set
			{
				if (FNoteSource != value)
				{
					FNoteSource = value;
					IsModify = true;
					NotePanic();
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
				IsModify = true;
				NotifyPropertyChanged(nameof(Settings));
			}
		}

		private SoundPointEvent FSoundControlAddPointAction = null;
		private SoundPoint FSelectedPoint = null;

		public bool IsModify
		{
			get => FIsModify;
			set
			{
				if (FIsModify != value)
				{
					FIsModify = value;
					NotifyPropertyChanged(nameof(IsModify));
					NotifyPropertyChanged(nameof(Title));
				}
			}
		}

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

			foreach (var p in r.Points)
				p.Waveform = r.CreateWaveform(p.WaveformName);

			r.FileName = AFileName;
			r.IsModify = false;
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
			IsModify = true;
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

		[XmlIgnore]
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

		public AdsrEnvelope Envelope
		{
			get
			{
				if (FEnvelope == null)
					FEnvelope = AdsrEnvelope.Fast;
				return FEnvelope;
			}
			set
			{
				IsModify = true;
				if (value != null)
					FEnvelope = value.Clone();
				else
					FEnvelope = null;
				NotifyPropertyChanged(nameof(Envelope));
			}
		}

		public void StartPlay(int ASampleRate, int AChannels)
		{
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(ASampleRate, AChannels);
			FContinueOneNote = new Note(null, WaveFormat, null, AdsrEnvelope.Fast);
			foreach (var p in Points)
				p.Waveform.Init(ASampleRate);

			//FWaveProvider = new MTNoteWaveProvider();
			FWaveProvider = App.Settings.Preferences.CreateNoteProvider();
			FWaveProvider.Init(WaveFormat);
		}

		public void StopPlay()
		{
			NotePanic();
			if (FWaveProvider is IDisposable d)
				d.Dispose();
			FWaveProvider = null;
		}

		public SoundPoint CreateDefaultPoint(double AFrequency, double AVolume)
		{
			SoundPoint p = new SoundPoint()
			{
				Frequency = AFrequency,
				Volume = AVolume
			};
			p.Waveform = DefaultWaveform;
			p.Waveform.Init(WaveFormat.SampleRate);
			return p;
		}

		public string Title
		{
			get
			{
				if (string.IsNullOrEmpty(FileName))
					return App.AppName;
				var m = (FIsModify) ? "*" : string.Empty;
				return $"{App.AppName} [{Path.GetFileNameWithoutExtension(FileName)}{m}]";
			}
		}
		
		public int Read(float[] buffer, int offset, int count)
		{
			long startRead = FDebugStopwatch.ElapsedMilliseconds;
			long startReadTicks = FDebugStopwatch.ElapsedTicks;

			int maxn = offset + count;

			Note[] notes = { };

			switch (FNoteSource)
			{
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

			//if (notes.Length > 0)
			//	Debug.WriteLine("Before SP.Read");

			FWaveProvider.Read(notes, buffer, offset, maxn);

			long endRead = FDebugStopwatch.ElapsedMilliseconds;
			long endReadTicks = FDebugStopwatch.ElapsedTicks;
			var dotsPerChannel = count / WaveFormat.Channels;
			var bufferTime = 1000 * dotsPerChannel / WaveFormat.SampleRate;
			var bufferTimeTicks = 1000 * dotsPerChannel / WaveFormat.SampleRate * TimeSpan.TicksPerMillisecond;

			if (endRead - startRead >= bufferTime)
				Debug.WriteLine("Overload! " + (endRead - startRead - bufferTime));

			if (DebugMode)
			{
				Debug.WriteLine($"NewStart - OldStart = {startRead - FOldStartRead}, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
				Debug.WriteLine($"Start {startRead}, end {endRead}, D: {endRead - startRead}, Count: {count}, CTime (msec): {bufferTime}, (Saple/Sec): {WaveFormat.SampleRate}");
			}

			lock (FFileWriterLock)
			{
				string rs = string.Empty;
				if (FFileWriter != null)
				{
					FFileWriter.WriteSamples(buffer, offset, count);
					rs = " Recording...";
				}

				//Interlocked.Exchange(ref FStatus, $"Load: {100 * (endRead - startRead) / bufferTime,3}% ({bufferTime}){rs}");
				Interlocked.Exchange(ref FStatus, $"Load: {100 * (endReadTicks - startReadTicks) / bufferTimeTicks, 3}% ({bufferTime}) {rs}");
			}

			//if (notes.Length > 0)
			//	Debug.WriteLine("After SP.Read");

			FOldStartRead = startRead;
			return count;
		}

		//public SoundPointValue GetValue(SoundPoint[] APoints, double ATime)
		//{
		//	double max = 0;
		//	SoundPointValue r = new SoundPointValue();

		//	foreach (var p in APoints)
		//	{
		//		if (p.IsMute)
		//			continue;

		//		max += p.Volume;
		//		var v = p.Volume * p.GetValue(ATime);

		//		if (p.IsSolo)
		//			return v;

		//		r += v;
		//	}

		//	if (max > 1)
		//		r /= max;

		//	return r;
		//}

		public SoundPointEvent SoundControlAddPointAction
		{
			get
			{
				if (FSoundControlAddPointAction == null)
					FSoundControlAddPointAction = new SoundPointEvent((sp) =>
					{
						//sp.Kind = NewPointKind;
						Points.Add(sp);
						IsModify = true;
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
				IsModify = false;
				NotifyPropertyChanged(nameof(Title));
			}
			catch (Exception ex)
			{
				App.ShowError(ex.Message);
			}
		}

		public void AddNote(object AKey, double AMultipler)
		{
			if ((NoteSource == NoteSourceEnum.ContinueOne) || (NoteSource == NoteSourceEnum.None))
				return;

			var a = Points.Select(p =>
			{
				p = p.Clone();
				p.Frequency *= AMultipler;
				p.IsNote = true;
				return p;
			}).ToArray();

			lock (FNotesLock)
			{
				FNotes.Add(new Note(a, WaveFormat, AKey, Envelope.Clone()));
			}
		}

		public void AddNoteByHalftone(object AKey, int AHalftoneOffset)
		{
			var v = Math.Pow(2, (double)AHalftoneOffset / 12);
			AddNote(AKey, v);
		}

		public bool DeleteNote(object AKey)
		{
			lock (FNotesLock)
				foreach (var n in FNotes)
					if ((n.Key.Equals(AKey)) && (n.Phase == NotePhase.Playing))
					{
						n.Phase = NotePhase.Stopping;
						return true;
					}
			return false;
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

		public void StartRecord(string AFileName)
		{
			if (FFileWriter != null)
				return;
			lock (FFileWriterLock)
			{
				FFileWriter = new WaveFileWriter(AFileName, WaveFormat);
			}
		}

		public void StopRecord()
		{
			lock (FFileWriterLock)
			{
				if (FFileWriter != null)
				{
					FFileWriter.Close();
					FFileWriter.Dispose();
					FFileWriter = null;
				}
			}
		}

		public Waveform[] BuildinWaveforms
		{
			get
			{
				if (FBuildinWaveforms == null)
				{
					FBuildinWaveforms = typeof(Waveform).Assembly.GetTypes().
						Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Waveform))).
						Select(tt => (Waveform)Activator.CreateInstance(tt)).ToArray();
				}
				return FBuildinWaveforms;
			}
		}

		public Waveform DefaultWaveform => BuildinWaveforms.First(wf => wf is SineWaveform);

		public Waveform[] Waveforms => BuildinWaveforms;

		public Waveform CreateWaveform(string AName)
		{
			var r = Waveforms.FirstOrDefault(wf => wf.Name == AName);
			if (r == null)
				r = DefaultWaveform;
			return r;
		}
	}
}
