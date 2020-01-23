using CommandLine;
using NAudio.Wave;
using SoundMap.Settings;
using SoundMap.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace SoundMap.Models
{
	public class MainWindowModel: Observable
	{
		private RelayCommand FOpenProjectCommand = null;
		private RelayCommand FSaveProjectCommand = null;
		private RelayCommand FSaveProjectAsCommand = null;
		private RelayCommand FRecentFileCommand = null;

		private RelayCommand FExitCommand = null;
		private RelayCommand FNewProjectCommand = null;

		private RelayCommand FIsPauseCommand = null;
		private RelayCommand FSetNewPointKindCommand = null;
		private RelayCommand FPreferencesCommand = null;
		private RelayCommand FProjectPropertiesCommand = null;

		private RelayCommand FStartRecordCommand = null;
		private RelayCommand FStopRecordCommand = null;

		private SoundProject FProject = new SoundProject();
		private bool FIsPause = false;
		private IWavePlayer FOutput = null;
		private readonly MainWindow FMainWindow;
		private readonly DispatcherTimer FStatusTimer;
		private readonly List<Key> FPressedKeys = new List<Key>();

		public AppSettings SettingsProxy => App.Settings;

		public MainWindowModel(MainWindow AMainWindow)
		{
			FMainWindow = AMainWindow;

			try
			{
				Parser.Default.ParseArguments<AppCommandLine>(App.Args)
				 .WithParsed<AppCommandLine>(cl =>
				 {
						if (File.Exists(cl.FileName))
							Project = SoundProject.CreateFromFile(App.Args[0]);
						else
							if (cl.Last && App.Settings.HasFileHistory)
								Project = SoundProject.CreateFromFile(App.Settings.FileHistory.First());
				 });
			}
			catch (Exception ex)
			{
				App.ShowError(ex.Message);
			}

			FStatusTimer = new DispatcherTimer();
			FStatusTimer.Interval = TimeSpan.FromMilliseconds(100);
			FStatusTimer.Tick += StatusTimer_Tick;
			
		}

		public void WindowLoaded()
		{
			StartPlay();
			FStatusTimer.Start();
		}

		public SoundProject Project
		{
			get => FProject;
			set
			{
				if (!IsPause)
					StopPlay();

				Interlocked.Exchange(ref FProject, value);

				if (!IsPause)
					StartPlay();

				NotifyPropertyChanged(nameof(Project));
			}
		}

		private void StartPlay()
		{
			if (FOutput != null)
				return;

			try
			{
				FOutput = App.Settings.Preferences.CreateOutput();
				if ((FProject != null) && (FOutput != null))
				{
					FProject.NotePanic();
					FProject.InitGenerator(App.Settings.Preferences.SampleRate, App.Settings.Preferences.Channels);
					FOutput.Init(FProject);
					//var sg = new SignalGenerator(App.Settings.Preferences.SampleRate, App.Settings.Preferences.Channels);
					//sg.Frequency = 220;
					//sg.Type = SignalGeneratorType.Sin;
					//FOutput.Init(sg);
					FOutput.Play();
				}
			}
			catch (Exception ex)
			{
				FOutput = null;
				App.ShowError(ex.Message);
			}
		}

		private void StopPlay()
		{
			if (FOutput != null)
			{
				FProject?.NotePanic();
				FOutput.Stop();
				FOutput.Dispose();
				FOutput = null;
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

		public void WindowClosing(CancelEventArgs e)
		{
			if (Project.IsModify)
				switch (MessageBox.Show("Project has been changed. Save?", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1))
				{
					case DialogResult.Yes:
						SaveProjectCommand.Execute(null);
						break;
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
					case DialogResult.No:
						break;
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
							{
								Project = SoundProject.CreateFromFile(dlg.FileName);
								App.Settings.AddHistory(dlg.FileName);
							}
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
							{
								try
								{
									Project.SaveToFile(dlg.FileName);
									App.Settings.AddHistory(dlg.FileName);
								}
								catch (Exception ex)
								{
									App.ShowError(ex.Message);
								}
							}
						}
					});
				return FSaveProjectAsCommand;
			}
		}

		public RelayCommand RecentFileCommand
		{
			get
			{
				if (FRecentFileCommand == null)
					FRecentFileCommand = new RelayCommand((obj) =>
					{
						try
						{
							var fn = (string)obj;
							Project = SoundProject.CreateFromFile(fn);
							App.Settings.AddHistory(fn);
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					});
				return FRecentFileCommand;
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

		public RelayCommand IsPauseCommand
		{
			get
			{
				if (FIsPauseCommand == null)
					FIsPauseCommand = new RelayCommand((obj) => IsPause = !IsPause);
				return FIsPauseCommand;
			}
		}

		public RelayCommand SetNewPointKindCommand
		{
			get
			{
				if (FSetNewPointKindCommand == null)
					FSetNewPointKindCommand = new RelayCommand((obj) =>
					{
						//Project.NewPointKind = (PointKind)obj;
					});
				return FSetNewPointKindCommand;
			}
		}

		public RelayCommand PreferencesCommand
		{
			get
			{
				if (FPreferencesCommand == null)
					FPreferencesCommand = new RelayCommand((param) =>
					{
						if (!IsPause)
							StopPlay();

						PreferencesWindow wnd = new PreferencesWindow();
						wnd.Owner = FMainWindow;
						wnd.DataContext = App.Settings.Preferences.Clone();
						if (wnd.ShowDialog() == true)
							App.Settings.Preferences = (PreferencesSettings)wnd.DataContext;

						if (!IsPause)
							StartPlay();
					});
				return FPreferencesCommand;
			}
		}

		public RelayCommand ProjectPropertiesCommand
		{
			get
			{
				if (FProjectPropertiesCommand == null)
					FProjectPropertiesCommand = new RelayCommand((param) =>
					{
						ProjectSettingsWindow wnd = new ProjectSettingsWindow();
						wnd.Owner = FMainWindow;
						wnd.DataContext = FProject.Settings.Clone();
						if (wnd.ShowDialog() == true)
							FProject.Settings = (ProjectSettings)wnd.DataContext;
					});
				return FProjectPropertiesCommand;
			}
		}

		public void KeyDown(System.Windows.Input.KeyEventArgs AKey)
		{
			if (FPressedKeys.IndexOf(AKey.Key) != -1)
				return;

			FPressedKeys.Add(AKey.Key);
			AKey.Handled = true;
			switch (AKey.Key)
			{
				case Key.F1:
					Project.DebugMode = true;
					break;
				case Key.Z:
					Project.AddNoteByHalftone(AKey.Key, -7);
					break;
				case Key.S: // 2
					Project.AddNoteByHalftone(AKey.Key, -6);
					break;
				case Key.X:
					Project.AddNoteByHalftone(AKey.Key, -5);
					break;
				case Key.C:
					Project.AddNoteByHalftone(AKey.Key, -3);
					break;
				case Key.V:
					Project.AddNoteByHalftone(AKey.Key, -2);
					break;
				case Key.G: // 2
					Project.AddNoteByHalftone(AKey.Key, -1);
					break;

				case Key.B:
					Project.AddNoteByHalftone(AKey.Key, 0);
					break;

				case Key.H: // 2
					Project.AddNoteByHalftone(AKey.Key, 1);
					break;
				case Key.N:
					Project.AddNoteByHalftone(AKey.Key, 2);
					break;
				case Key.M:
					Project.AddNoteByHalftone(AKey.Key, 3);
					break;
				case Key.OemComma:
					Project.AddNoteByHalftone(AKey.Key, 5);
					break;
				case Key.OemPeriod:
					Project.AddNoteByHalftone(AKey.Key, 7);
					break;
				case Key.OemQuestion:
					Project.AddNoteByHalftone(AKey.Key, 9);
					break;

				default:
					AKey.Handled = false;
					break;
			}
		}

		public void KeyUp(System.Windows.Input.KeyEventArgs AKey)
		{
			AKey.Handled = true;
			switch (AKey.Key)
			{
				case Key.F1:
					Project.DebugMode = false;
					return;
				case Key.Space:
					Project.NotePanic();
					return;
				default:
					AKey.Handled = false;
					break;
			}

			FPressedKeys.Remove(AKey.Key);
			AKey.Handled = Project.DeleteNote(AKey.Key);
		}

		public string Status
		{
			get
			{
				if (FOutput != null)
					return $"Playing; {FProject.WaveFormat.SampleRate}, {FProject.WaveFormat.Channels}; {FProject.Status}";
				return "Stopped;";
			}
		}

		private void StatusTimer_Tick(object sender, EventArgs e)
		{
			NotifyPropertyChanged(nameof(Status));
		}

		public RelayCommand StartRecordCommand
		{
			get
			{
				if (FStartRecordCommand == null)
					FStartRecordCommand = new RelayCommand((obj) =>
					{
						using (SaveFileDialog dlg = new SaveFileDialog())
						{
							if (!string.IsNullOrEmpty(Project.FileName))
							{
								dlg.InitialDirectory = Path.GetFullPath(Project.FileName);
								dlg.FileName = Path.GetFileName(Project.FileName);
							}
							dlg.Filter = "Wave file (*.wav)|*.wav";
							dlg.FilterIndex = 1;
							if (dlg.ShowDialog() == DialogResult.OK)
								FProject.StartRecord(dlg.FileName);
						}
					});
				return FStartRecordCommand;
			}
		}

		public RelayCommand StopRecordCommand
		{
			get
			{
				if (FStopRecordCommand == null)
					FStopRecordCommand = new RelayCommand((obj) =>
					{
						Project.StopRecord();
					});
				return FStopRecordCommand;
			}
		}
	}
}
