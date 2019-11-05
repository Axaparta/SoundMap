using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SoundMap
{

	#region class DeviceMenuItem
	public class DeviceMenuItem
	{
		public string Name => Device.FriendlyName;
		public MMDevice Device { get; }

		public bool Selected { get; set; }

		public DeviceMenuItem(MMDevice ADevice, bool ASelected)
		{
			Device = ADevice;
			Selected = ASelected;
		}
	}
	#endregion

	public class MainWindowModel: Observable
	{
		private MMDevice[] FDevices = null;
		private readonly string FDefaultDeviceId = null;
		private WasapiOut FOut = null;

		private RelayCommand FOpenProjectCommand = null;
		private RelayCommand FSaveProjectCommand = null;
		private RelayCommand FSaveProjectAsCommand = null;
		private RelayCommand FExitCommand = null;
		private RelayCommand FNewProjectCommand = null;
		private RelayCommand FDeviceMenuItemCommand = null;
		private RelayCommand FIsPauseCommand = null;

		private SoundProject FProject = new SoundProject();
		private bool FIsPause = false;

		public MainWindowModel()
		{
			using (var e = new MMDeviceEnumerator())
			{
				FDefaultDeviceId = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
				FDevices = e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArray();
			}
			StartPlay();
		}

		public SoundProject Project
		{
			get => FProject;
			set
			{
				if (!IsPause)
					StopPlay();

				FProject = value;

				if (!IsPause)
					StartPlay();

				NotifyPropertyChanged(nameof(Project));
			}
		}

		private void StartPlay()
		{
			var sd = SelectedDevice;
			if ((FProject != null) && (sd != null))
			{
				FProject.ConfigureGenerator(sd.AudioClient.MixFormat.SampleRate, sd.AudioClient.MixFormat.Channels);
				FOut = new WasapiOut(sd, AudioClientShareMode.Shared, true, 25);
				FOut.Init(FProject);
				FOut.Play();
			}
		}

		private void StopPlay()
		{
			if (FOut != null)
			{
				FOut.Stop();
				FOut.Dispose();
				FOut = null;
			}
		}

		public DeviceMenuItem[] Devices
		{
			get
			{
				var r = FDevices.Select(d => new DeviceMenuItem(d, d.ID == SelectedDeviceId)).ToArray();
				return r;
			}
		}

		public string SelectedDeviceId
		{
			get
			{
				var dev = FDevices.FirstOrDefault(d => d.ID == App.Settings.DeviceId);
				if (dev == null)
					return FDefaultDeviceId;
				return dev.ID;
			}
			set
			{
				App.Settings.DeviceId = value;
			}
		}

		public MMDevice SelectedDevice
		{
			get
			{
				var dn = SelectedDeviceId;
				return FDevices.FirstOrDefault(d => d.ID == dn);
			}
		}

		public bool IsPause
		{
			get => FIsPause;
			set
			{
				if (FIsPause != value)
				{
					FIsPause = value;
					if (FIsPause)
						StopPlay();
					else
						StartPlay();
					NotifyPropertyChanged(nameof(IsPause));
				}
			}
		}

		public void WindowClose()
		{
			StopPlay();
		}

		public RelayCommand ExitCommand
		{
			get
			{
				if (FExitCommand == null)
					FExitCommand = new RelayCommand((obj) => System.Windows.Application.Current.MainWindow.Close());
				return FExitCommand;
			}
		}

		public RelayCommand OpenProjectCommand
		{
			get
			{
				if (FOpenProjectCommand == null)
					FOpenProjectCommand = new RelayCommand((obj) =>
					{
						try
						{
							OpenFileDialog dlg = new OpenFileDialog();
							dlg.Filter = SoundProject.FileFilter;
							if (dlg.ShowDialog() == DialogResult.OK)
								Project = SoundProject.CreateFromFile(dlg.FileName);
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					});
				return FOpenProjectCommand;
			}
		}

		public RelayCommand SaveProjectCommand
		{
			get
			{
				if (FSaveProjectCommand == null)
					FSaveProjectCommand = new RelayCommand((obj) =>
					{
						if (string.IsNullOrEmpty(Project.FileName))
							SaveProjectAsCommand.Execute(null);
						else
							Project.SaveToFile(Project.FileName);
					});
				return FSaveProjectCommand;
			}
		}

		public RelayCommand SaveProjectAsCommand
		{
			get
			{
				if (FSaveProjectAsCommand == null)
					FSaveProjectAsCommand = new RelayCommand((obj) =>
					{
						using (SaveFileDialog dlg = new SaveFileDialog())
						{
							if (!string.IsNullOrEmpty(Project.FileName))
							{
								dlg.InitialDirectory = Path.GetFullPath(Project.FileName);
								dlg.FileName = Path.GetFileName(Project.FileName);
							}
							dlg.Filter = SoundProject.FileFilter;
							dlg.FilterIndex = 1;
							if (dlg.ShowDialog() == DialogResult.OK)
								Project.SaveToFile(dlg.FileName);
						}
					});
				return FSaveProjectAsCommand;
			}
		}

		public RelayCommand NewProjectCommand
		{
			get
			{
				if (FNewProjectCommand == null)
					FNewProjectCommand = new RelayCommand((obj) =>
					{
						Project = new SoundProject();
					});
				return FNewProjectCommand;
			}
		}

		public RelayCommand DeviceMenuItemCommand
		{
			get
			{
				if (FDeviceMenuItemCommand == null)
					FDeviceMenuItemCommand = new RelayCommand((obj) =>
					{
						if (!IsPause)
							StopPlay();

						SelectedDeviceId = (obj as DeviceMenuItem).Device.ID;
						
						if (!IsPause)
							StartPlay();

						NotifyPropertyChanged(nameof(Devices));
					});
				return FDeviceMenuItemCommand;
			}
		}

		public RelayCommand IsPauseCommand
		{
			get
			{
				if (FIsPauseCommand == null)
					FIsPauseCommand = new RelayCommand((obj) => IsPause = !IsPause);
				return FIsPauseCommand;
			}
		}
	}
}
