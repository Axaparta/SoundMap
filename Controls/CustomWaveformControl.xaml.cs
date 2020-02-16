using SoundMap.Waveforms;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SoundMap.Controls
{
	public partial class CustomWaveformControl : UserControl
	{
		private RelayCommand FAddWaveformCommand = null;
		private RelayCommand FApplyWaveformCommand = null;

		public CustomWaveformControl()
		{
			//DataContext = this;
			InitializeComponent();

			Loaded += (s, e) => CommandManager.InvalidateRequerySuggested();
		}

		public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
			"Project", typeof(SoundProject), typeof(CustomWaveformControl));

		public SoundProject Project
		{
			get => (SoundProject)GetValue(ProjectProperty);
			set => SetValue(ProjectProperty, value);
		}

		//public static readonly DependencyProperty CustomWaveformsProperty = DependencyProperty.Register(
		//	"CustomWaveforms", typeof(ObservableCollection<CustomWaveform>), typeof(CustomWaveformControl), new FrameworkPropertyMetadata(new ObservableCollection<CustomWaveform>()));

		//public ObservableCollection<CustomWaveform> CustomWaveforms
		//{
		//	get => (ObservableCollection<CustomWaveform>)GetValue(CustomWaveformsProperty);
		//	set => SetValue(CustomWaveformsProperty, value);
		//}

		public static readonly DependencyProperty SelectedWaveformProperty = DependencyProperty.Register(
			"SelectedWaveform", typeof(CustomWaveform), typeof(CustomWaveformControl), new FrameworkPropertyMetadata(new CustomWaveform(), SelectedWaveformChanged));

		public CustomWaveform SelectedWaveform
		{
			get => (CustomWaveform)GetValue(SelectedWaveformProperty);
			set => SetValue(SelectedWaveformProperty, value);
		}

		private static void SelectedWaveformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//CustomWaveformControl c = (CustomWaveformControl)d;
			//if (e.NewValue != null)
			//	c.CloneSelectedWaveform = (CustomWaveform)((CustomWaveform)e.NewValue).Clone();
			//else
			//	c.CloneSelectedWaveform = null;
		}

		public RelayCommand AddWaveformCommand
		{
			get
			{
				if (FAddWaveformCommand == null)
					FAddWaveformCommand = new RelayCommand((param) =>
					{
						try
						{
							if (string.IsNullOrEmpty(NamesComboBox.Text))
								throw new Exception("Need to enter the name of waveform");
							CustomWaveform wf = new CustomWaveform(NamesComboBox.Text, WaveformConturControl.OneHerz);
							Project.Waveforms.Add(wf);
							SelectedWaveform = wf;
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					}, (param) =>
					{
						if ((NamesComboBox != null) && (Project != null))
							return (NamesComboBox.Text.Length > 0) && (Project.CustomWaveforms.FirstOrDefault(cwf => cwf.Name == NamesComboBox.Text) == null);
						return false;
					});
				return FAddWaveformCommand;
			}
		}

		public RelayCommand ApplyWaveformCommand
		{
			get
			{
				if (FApplyWaveformCommand == null)
					FApplyWaveformCommand = new RelayCommand((param) =>
					{
						SelectedWaveform.OneHerz = SelectedWaveform.OneHerz;
					}, (param) =>
					{
						if (NamesComboBox != null)
							return (SelectedWaveform != null) && (NamesComboBox.Text.Length > 0);
						return false;
					});
				return FApplyWaveformCommand;
			}
		}

		private void NamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//SelectedWaveform = e.Source
			Debug.WriteLine(e.Source);
		}

		private void NamesComboBox_TextInput(object sender, TextCompositionEventArgs e)
		{
			CommandManager.InvalidateRequerySuggested();
		}
	}
}
