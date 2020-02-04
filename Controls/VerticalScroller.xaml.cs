using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DeepSound.Controls
{
	public partial class VerticalScroller: UserControl
	{
		private bool FIsMouseDown = false;

		public VerticalScroller()
		{
			IsTabStop = true;
			Focusable = true;
			InitializeComponent();
			ControlPanel.DataContext = this;
		}

		private void ValueCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var left = (ValueCanvas.ActualWidth - BackRectangle.ActualWidth)/2;
			Canvas.SetLeft(BackRectangle, left);
			BackRectangle.Height = ValueCanvas.ActualHeight;

			Canvas.SetLeft(FrontRectangle, left);
			Canvas.SetLeft(HolderEllipse, (ValueCanvas.ActualWidth - HolderEllipse.ActualWidth)/2);

			ValueReposition();
		}

		private void ValueReposition()
		{
			if (Value > MaxValue)
			{
				Value = MaxValue;
				return;
			}
			if (Value < MinValue)
			{
				Value = MinValue;
				return;
			}

			var bh = (ValueCanvas.ActualHeight - HolderEllipse.ActualHeight)*(Value - MinValue)/(MaxValue - MinValue) + HolderEllipse.ActualHeight/2;

			if (bh >= 0)
			{
				FrontRectangle.Height = bh;
				Canvas.SetBottom(HolderEllipse, bh - HolderEllipse.ActualHeight/2);
			}
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged));

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			"MinValue", typeof(double), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged));

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			"MaxValue", typeof(double), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender, ValuePropertyChanged));

		private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as VerticalScroller).ValueReposition();
		}

		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public double MinValue
		{
			get { return (double)GetValue(MinValueProperty); }
			set { SetValue(MinValueProperty, value); }
		}

		public double MaxValue
		{
			get { return (double)GetValue(MaxValueProperty); }
			set { SetValue(MaxValueProperty, value); }
		}

		public static readonly DependencyProperty ValueForegroundProperty = DependencyProperty.Register(
			"ValueForeground", typeof(Brush), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(SystemColors.ControlDarkDarkBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush ValueForeground
		{
			get { return (Brush)GetValue(ValueForegroundProperty); }
			set { SetValue(ValueForegroundProperty, value); }
		}

		public static readonly DependencyProperty ValueBackgroundProperty = DependencyProperty.Register(
			"ValueBackground", typeof(Brush), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(SystemColors.ControlLightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush ValueBackground
		{
			get { return (Brush)GetValue(ValueBackgroundProperty); }
			set { SetValue(ValueBackgroundProperty, value); }
		}

		public static readonly DependencyProperty FocusForegroundProperty = DependencyProperty.Register(
			"FocusForeground", typeof(Brush), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(SystemColors.ControlLightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush FocusForeground
		{
			get { return (Brush)GetValue(FocusForegroundProperty); }
			set { SetValue(FocusForegroundProperty, value); }
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			"Title", typeof(string), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, StaticTitleChanged));

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty ValueWidthProperty = DependencyProperty.Register(
			"ValueWidth", typeof(double), typeof(VerticalScroller),
			new FrameworkPropertyMetadata((double)6, FrameworkPropertyMetadataOptions.AffectsRender, ValueWidthChange));

		public double ValueWidth
		{
			get { return (double)GetValue(ValueWidthProperty); }
			set { SetValue(ValueWidthProperty, value); }
		}

		public static readonly DependencyProperty HasFocusedParentProperty = DependencyProperty.Register(
			"HasFocusedParent", typeof(bool), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(false));

		[Description("Флаг использования свойства ParentIsFocused")]
		public bool HasFocusedParent
		{
			get
			{
				return (bool)GetValue(HasFocusedParentProperty);
			}
			set
			{
				SetValue(HasFocusedParentProperty, value);
			}
		}

		public static readonly DependencyProperty ParentIsFocusedProperty = DependencyProperty.Register(
			"ParentIsFocused", typeof(bool), typeof(VerticalScroller),
			new FrameworkPropertyMetadata(false));

		[Description("Флаг того, что родитель имеет фокус")]
		public bool ParentIsFocused
		{
			get
			{
				return (bool)GetValue(ParentIsFocusedProperty);
			}
			set
			{
				SetValue(ParentIsFocusedProperty, value);
			}
		}

		private static void ValueWidthChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var v = (double)e.NewValue;
			var vs = d as VerticalScroller;
			vs.MinWidth = Math.Max(vs.TitleLabel.ActualWidth, v*5);
			var ew = v*3;
			vs.HolderEllipse.Width = ew;
			vs.HolderEllipse.Height = ew;
			vs.ValueReposition();
		}

		private static void StaticTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as VerticalScroller).TitleLabel.Visibility = (string.IsNullOrEmpty((string)e.NewValue))? Visibility.Collapsed: Visibility.Visible;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			FIsMouseDown = (e.ChangedButton == MouseButton.Left);
			if (FIsMouseDown)
			{
				if ((HasFocusedParent && ParentIsFocused) || (!HasFocusedParent))
				{
					Focus();
					OnMouseMove(e);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (FIsMouseDown)
			{
				var p = e.GetPosition(ValueCanvas);
				Value = MaxValue - (p.Y - HolderEllipse.Height/2)/(ValueCanvas.ActualHeight - HolderEllipse.Height)*(MaxValue - MinValue);
				e.Handled = true;
				base.OnMouseMove(e);
			}
		}

		protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				FIsMouseDown = false;
				e.Handled = true;
				base.OnMouseUp(e);
			}
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			FIsMouseDown = false;
			e.Handled = true;
			base.OnMouseLeave(e);
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			Focus();
			Value += Math.Sign(e.Delta)*(MaxValue - MinValue)*0.01;
			e.Handled = true;
			base.OnMouseWheel(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Up:
				case Key.Right:
					Value += (MaxValue - MinValue) * 0.01;
					break;
				case Key.Down:
				case Key.Left:
					Value -= (MaxValue - MinValue) * 0.01;
					break;
				case Key.PageUp:
					Value += (MaxValue - MinValue) * 0.1;
					break;
				case Key.PageDown:
					Value -= (MaxValue - MinValue) * 0.1;
					break;
				case Key.Home:
					Value = MaxValue;
					break;
				case Key.End:
					Value = MinValue;
					break;
				default:
					e.Handled = false;
					break;
			}
			base.OnKeyDown(e);
		}
	}
}
