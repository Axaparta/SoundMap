using Common;
using NAudio.Wave;
using SoundMap.NoteWaveProviders;
using SoundMap.Settings;
using SoundMap.Temperaments;
using SoundMap.Waveforms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SoundMap
{
	[Serializable]
	public class SoundProject: Observable, ISampleProvider
	{
		private static readonly Temperament[] StaticTemperaments = new Temperament[] { new EqualTemperament(), new CleanTemperament(), new PifagorTemperament(1) };

		public static readonly string FileFilter = "SoundMap project (*.smp)|*.smp";


		private ProjectSettings FSettings = new ProjectSettings();
		private NoteSourceEnum FNoteSource = NoteSourceEnum.ContinueOne;

		private WaveFileWriter FFileWriter = null;
		private readonly object FFileWriterLock = new object();

		private readonly List<Note> FNotes = new List<Note>();
		private readonly object FNotesLock = new object();
		private Note FContinueOneNote = null;
		private string FStatus;
		private AdsrEnvelope FEnvelope = null;
		private NoteWaveProvider FWaveProvider = null;
		private double FMasterVolume = 1;
		private double FLVolume = 0;
		private double FRVolume = 0;
		private DispatcherTimer FVolumeTimer;
		private Temperament FTemperament = null;

		/// <summary>
		/// Содержит все вафформы. CustomWaveforms выбирается из него
		/// </summary>
		[XmlIgnore]
		public ObservableCollection<Waveform> Waveforms { get; } = new ObservableCollection<Waveform>();

		[XmlIgnore]
		public WaveFormat WaveFormat { get; private set; }

		[XmlIgnore]
		public SoundPointCollection Points { get; } = new SoundPointCollection();

		[XmlIgnore]
		public SoundPointCollection SelectedPoints { get; } = new SoundPointCollection();

		[XmlIgnore]
		public string FileName { get; set; }

		private readonly Stopwatch FDebugStopwatch = new Stopwatch();
		private long FOldStartRead = 0;
		private bool FIsModify = false;

		public double MasterVolume
		{
			get => FMasterVolume;
			set
			{
				if (FMasterVolume != value)
				{
					FMasterVolume = value;
					IsModify = true;
					NotifyPropertyChanged(() => MasterVolume);
				}
			}
		}

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

			FVolumeTimer = new DispatcherTimer();
			FVolumeTimer.Tick += new EventHandler(VolumeTimerTick);
			FVolumeTimer.Interval = TimeSpan.FromMilliseconds(50);

			Waveforms.Add(new SineWaveform());
			Waveforms.Add(new BufferSineWaveForm());
			Waveforms.Add(new BufferSawLWaveForm());

			Waveforms.CollectionChanged += Waveforms_CollectionChanged;

			FDebugStopwatch.Start();
		}

		private void Waveforms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			NotifyPropertyChanged(nameof(CustomWaveforms));
		}

		public static SoundProject CreateFromFile(string AFileName)
		{
			var r = XmlHelper.Load<SoundProject>(AFileName);

			foreach (var p in r.Points)
				p.Project = r;

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
			NotifyPropertyChanged(nameof(PointsInfo));
		}

		private void Points_PointPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
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
			//foreach (var p in Points)
			//	p.Waveform.Init(ASampleRate);
			//foreach (var wf in Waveforms)
			//	wf.Init(ASampleRate);

			FWaveProvider = App.Settings.Preferences.CreateNoteProvider();

			FWaveProvider.Init(WaveFormat);
			FVolumeTimer.Start();
		}

		public void StopPlay()
		{
			FVolumeTimer.Stop();
			LVolume = RVolume = 0;
			NotePanic();
			if (FWaveProvider is IDisposable d)
				d.Dispose();
			FWaveProvider = null;
		}

		public SoundPoint CreateDefaultPoint(double AFrequency, double AVolume)
		{
			return new SoundPoint(this, AFrequency, AVolume);
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
			long beginTicks = FDebugStopwatch.ElapsedTicks;
			long beginTime = FDebugStopwatch.ElapsedMilliseconds;

			int maxn = offset + count;

			Note[] notes = { };

			foreach (var wf in Waveforms)
				wf.Init(WaveFormat.SampleRate);

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

			var beginRead = FDebugStopwatch.ElapsedMilliseconds;

			var ss = Waveforms.OfType<BufferWaveform>().Select(bfw => new KeyValuePair<int, double[]>(bfw.GetHashCode(), bfw.Sample)).ToArray();

			NoteWaveArgs a = new NoteWaveArgs(ss, FMasterVolume);

			FWaveProvider.Read(notes, buffer, offset, maxn, a);

			var endRead = FDebugStopwatch.ElapsedMilliseconds;

			string isRecording = string.Empty;
			lock (FFileWriterLock)
			{
				if (FFileWriter != null)
				{
					FFileWriter.WriteSamples(buffer, offset, count);
					isRecording = " Recording... ";
				}
			}

			var endTicks = FDebugStopwatch.ElapsedTicks;
			var endTime = FDebugStopwatch.ElapsedMilliseconds;
			// Семплов на один канал в буфере
			var dotsPerChannel = count / WaveFormat.Channels;
			// Сколько миллисекунд в буфере
			var bufferTime = 1000 * dotsPerChannel / WaveFormat.SampleRate;
			// Сколько тиков в буфере
			var bufferTimeTicks = 1000 * dotsPerChannel / WaveFormat.SampleRate * TimeSpan.TicksPerMillisecond;

			Interlocked.Exchange(ref FStatus, $"{FWaveProvider.Name}, load: {100 * (endTicks - beginTicks) / bufferTimeTicks, 3}% ({bufferTime}) {isRecording}");
			Interlocked.Exchange(ref FLVolume, a.MaxL);
			Interlocked.Exchange(ref FRVolume, a.MaxR);

			if (endTime - beginTime >= bufferTime)
				Debug.WriteLine("Overload! " + (endRead - beginTime - bufferTime));

			if (App.DebugMode)
			{
				//Debug.WriteLine($"NewStart - OldStart = {beginTime - FOldStartRead}, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
				//Debug.WriteLine($"Project.Read tot: {endTime - beginTime}, Count: {count}, CTime (msec): {bufferTime}, (Saple/Sec): {WaveFormat.SampleRate}, read: {afterRead - readBefore}");
				Debug.WriteLine($"Project.Read tot1: {endTime - beginTime}, tot2: {FDebugStopwatch.ElapsedMilliseconds - beginTime}, befR: {beginRead - beginTime}, aftR: {endTime - endRead}");
			}

			FOldStartRead = beginTime;
			return count;
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

		public void AddNote(object AKey, double AMultipler, double AVolume)
		{
			if ((NoteSource == NoteSourceEnum.ContinueOne) || (NoteSource == NoteSourceEnum.None))
				return;

			var a = Points.Select(p =>
			{
				p = p.Clone();
				p.Frequency *= AMultipler;
				return p;
			}).ToArray();

			lock (FNotesLock)
			{
				FNotes.Add(new Note(a, WaveFormat, AKey, Envelope.Clone(), AVolume));
			}
		}

		public void AddNoteByHalftone(object AKey, int AHalftoneOffset, double AVolume = 1)
		{
			//var v = Math.Pow(2, (double)AHalftoneOffset / 12);
			var t = Temperament.GetKeyboardTone(AHalftoneOffset);
			if (t != null)
				AddNote(AKey, t.Frequency, AVolume);
		}

		public int? GetHalftoneFromMidiNoteNumber(int ANoteIndex)
		{
			return ANoteIndex - 60;
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

		public Waveform GetWaveform(string AName)
		{
			var r = Waveforms.FirstOrDefault(wf => wf.Name == AName);
			if (r == null)
				r = Waveforms.First();
			return r;
		}

		[XmlIgnore]
		public double LVolume
		{
			get => FLVolume;
			set
			{
				if (FLVolume != value)
				{
					FLVolume = value;
					NotifyPropertyChanged(nameof(LVolume));
				}
			}
		}

		[XmlIgnore]
		public double RVolume
		{
			get => FRVolume;
			set
			{
				if (FRVolume != value)
				{
					FRVolume = value;
					NotifyPropertyChanged(nameof(RVolume));
				}
			}
		}

		private void VolumeTimerTick(object sender, EventArgs e)
		{
			NotifyPropertyChanged(nameof(LVolume));
			NotifyPropertyChanged(nameof(RVolume));
		}

		/// <summary>
		/// For statusbar
		/// </summary>
		public string PointsInfo
		{
			get => $"Point count: {Points.Count}";
		}

		/// <summary>
		/// Для сериализации и отображения списка в CustomWaveformcontrol
		/// </summary>
		public CustomWaveform[] CustomWaveforms
		{
			get => Waveforms.Where(wf => wf is CustomWaveform).Cast<CustomWaveform>().ToArray();
			set
			{
				List<Waveform> customWf = new List<Waveform>();
				customWf.AddRange(Waveforms.Where(wf => wf is CustomWaveform));
				foreach (var cwf in customWf)
					Waveforms.Remove(cwf);

				if (value != null)
					foreach (var v in value)
						Waveforms.Add(v);
			}
		}

		public string TemperamentName { get; set; }

		[XmlIgnore]
		public Temperament Temperament
		{
			get
			{
				if (FTemperament == null)
				{
					FTemperament = StaticTemperaments.FirstOrDefault(t => t.Name == TemperamentName);
					if (FTemperament == null)
						FTemperament = StaticTemperaments.First();
				}
				return FTemperament;
			}
			set
			{
				if (TemperamentName != value.Name)
				{
					TemperamentName = value.Name;
					FTemperament = value;
					NotifyPropertyChanged(nameof(Temperament));
					NotifyPropertyChanged(nameof(TemperamentName));
					IsModify = true;
				}
			}
		}

		[XmlIgnore]
		public Temperament[] Temperaments
		{
			get => StaticTemperaments;
		}
	}
}
