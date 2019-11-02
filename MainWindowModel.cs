using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;

namespace SoundMap
{
	public class MainWindowModel: Observable
	{
		private MMDevice FDevice = null;
		private MMDevice[] FDevices = null;
		private RelayCommand FStartStopCommand = null;
		private RelayCommand FLoadCommand = null;
		private RelayCommand FSaveCommand = null;
		private RelayCommand FClearCommand = null;

		private SoundGenerator FGenerator = null;
		private WasapiOut FOut = null;
		private Action<SoundPoint> FSoundControlAddPoint = null;
		private Action<SoundPoint> FSoundControlDeletePoint = null;

		public SoundPointCollection Points { get; } = new SoundPointCollection();

		public MainWindowModel()
		{
			using (var e = new MMDeviceEnumerator())
			{
				FDevice = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
				FDevices = e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArray();
			}

			Points.CollectionChanged += Points_CollectionChanged;
		}

		private void Points_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (FGenerator != null)
				FGenerator.SetPoints(Points);
		}

		public string[] Devices => FDevices.Select(d => d.FriendlyName).ToArray();

		public string SelectedDevice
		{
			get => FDevice.FriendlyName;
			set
			{
				FDevice = FDevices.FirstOrDefault(d => d.FriendlyName == value);
			}
		}

		public RelayCommand StartStopCommand
		{
			get
			{
				if (FStartStopCommand == null)
				{
					FStartStopCommand = new RelayCommand((obj) =>
					{
						if (FGenerator == null)
						{
							//FGenerator = new SoundGenerator(FDevice.AudioClient.MixFormat);
							FGenerator = new SoundGenerator(WaveFormat.CreateIeeeFloatWaveFormat(FDevice.AudioClient.MixFormat.SampleRate, 2));
							FGenerator.SetPoints(Points);
							FOut = new WasapiOut(FDevice, AudioClientShareMode.Shared, true, 25);
							FOut.Init(FGenerator);
							FOut.Play();
						}
						else
						{
							FOut.Stop();
							FOut.Dispose();
							FGenerator = null;
						}
					});
				}
				return FStartStopCommand;
			}
		}

		public void WindowClose()
		{
			if (FGenerator != null)
				StartStopCommand.Execute(null);
		}

		public Action<SoundPoint> SoundControlAddPoint
		{
			get
			{
				if (FSoundControlAddPoint == null)
				{
					FSoundControlAddPoint = new Action<SoundPoint>((sp) =>
					{
						Points.AddSoundPoint(sp);
					});
				}
				return FSoundControlAddPoint;
			}
		}

		public Action<SoundPoint> SoundControlDeletePoint
		{
			get
			{
				if (FSoundControlDeletePoint == null)
				{
					FSoundControlDeletePoint = new Action<SoundPoint>((sp) =>
					{
						Points.RemoveSoundPoint(sp);
					});
				}
				return FSoundControlDeletePoint;
			}
		}

		public RelayCommand LoadCommand
		{
			get
			{
				if (FLoadCommand == null)
					FLoadCommand = new RelayCommand((obj) =>
					{
						try
						{
							OpenFileDialog dlg = new OpenFileDialog();
							dlg.Filter = "SoundMap files (*.smx)|*.smx";
							if (dlg.ShowDialog() == DialogResult.OK)
							{
								var pts = XmlHelper.Load<SoundPoint[]>(dlg.FileName);
								Points.Clear();
								foreach (var p in pts)
									Points.AddSoundPoint(p);
							}
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					});
				return FLoadCommand;
			}
		}

		public RelayCommand SaveCommand
		{
			get
			{
				if (FSaveCommand == null)
					FSaveCommand = new RelayCommand((obj) =>
					{
						try
						{
							SaveFileDialog dlg = new SaveFileDialog();
							dlg.Filter = "SoundMap files (*.smx)|*.smx";
							dlg.FilterIndex = 1;
							if (dlg.ShowDialog() == DialogResult.OK)
								XmlHelper.Save(Points.ToArray(), dlg.FileName);
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					});
				return FSaveCommand;
			}
		}

		public RelayCommand ClearCommand
		{
			get
			{
				if (FClearCommand == null)
					FClearCommand = new RelayCommand((obj) =>
					{
						Points.Clear();
					});
				return FClearCommand;
			}
		}

	}
}
