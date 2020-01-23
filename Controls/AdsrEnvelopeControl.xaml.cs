using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SoundMap.Controls
{
	/// <summary>
	/// Логика взаимодействия для AdsrControl.xaml
	/// </summary>
	public partial class AdsrEnvelopeControl : UserControl
	{
		public AdsrEnvelopeControl()
		{
			InitializeComponent();
		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
				(sender as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}

		#region Envelope
		public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
			"Envelope", typeof(AdsrEnvelope), typeof(AdsrEnvelopeControl));

		public AdsrEnvelope Envelope
		{
			get => (AdsrEnvelope)GetValue(EnvelopeProperty);
			set => SetValue(EnvelopeProperty, value);
		}
		#endregion


		#region Project
		public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
			"Project", typeof(SoundProject), typeof(AdsrEnvelopeControl));

		public SoundProject Project
		{
			get => (SoundProject)GetValue(ProjectProperty);
			set => SetValue(ProjectProperty, value);
		}
		#endregion

	}
}
