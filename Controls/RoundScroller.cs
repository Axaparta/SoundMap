using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DeepSound.Controls
{
	public class RoundScroller: Control
	{
		internal static double PixelsPerDip { get; private set; }

		private bool FIsMouseDown = false;
		private Point FCenter = new Point();
		private double FLeftRightTab2 = 0;

		public RoundScroller()
			: base()
		{
			IsTabStop = true;
			Focusable = true;
			MinHeight = 20;
			MinWidth = 20;
			PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
		}

		private FormattedText CreateFormattedText(string AText, Brush ATextBrush)
		{
			return new FormattedText(
				AText,
				CultureInfo.CurrentUICulture,
				this.FlowDirection,
				new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
				this.FontSize,
				ATextBrush, PixelsPerDip);
		}

		private double PercentToAngle(double APercent)
		{
			var angle = -90 - FLeftRightTab2 - APercent*(360 - 2*FLeftRightTab2);
			return angle*Math.PI/180;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			var b = new Rect(
				Padding.Left, Padding.Top, constraint.Width - Padding.Left - Padding.Right,
				constraint.Height - Padding.Top - Padding.Bottom);

			var ft = CreateFormattedText("1", Brushes.Transparent);

			var textHeight = ft.Height;
			double titleTab = string.IsNullOrEmpty(Title)? 0: (textHeight + 2);

			var bigRadiusH = (b.Height - textHeight - titleTab)/2 - ValueWidth;
			var bigRadiusW = b.Width/2 - ValueWidth;

			var bigRadius = Math.Min(bigRadiusH, bigRadiusW);

			return new Size(constraint.Width, 2*(bigRadius + ValueWidth) + textHeight + titleTab);
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			var b = new Rect(Padding.Left, Padding.Top, ActualWidth - Padding.Left - Padding.Right, ActualHeight - Padding.Top - Padding.Bottom);
			drawingContext.DrawRectangle(Background, null, b);

			var ft = CreateFormattedText("1", Brushes.Transparent);

			var textHeight = ft.Height;
			double titleTab = string.IsNullOrEmpty(Title)? 0: (textHeight + 2);
			
			var bigRadiusH = (b.Height - textHeight - titleTab)/2 - ValueWidth;
			var bigRadiusW = b.Width/2 - ValueWidth;
			var bigRadius = Math.Min(bigRadiusH, bigRadiusW);

			var smallRadius = bigRadius - ValueWidth;
			var middleRadius = (bigRadius + smallRadius)/2;
			
			FLeftRightTab2 = 180*Math.Acos(1 - 3*ValueWidth/middleRadius/middleRadius)/Math.PI;
			FCenter = new Point(b.Left + b.Width/2, b.Top + titleTab + ValueWidth + bigRadius);

			var maxminDelta = MaxValue - MinValue;
			double valuePct = (maxminDelta == 0)? 0: (Value - MinValue)/maxminDelta;
			if (valuePct > 1)
				valuePct = 1;
			if (valuePct < 0)
				valuePct = 0;

			Func<double, double, Point> getArcPoint = (double APct, double ARadius) =>
				{
					var angle = PercentToAngle(APct);
					return new Point(
						FCenter.X + ARadius*Math.Cos(angle),
						FCenter.Y - ARadius*Math.Sin(angle));
				};

			var bigStart = getArcPoint(0, bigRadius);
			var smallEnd = getArcPoint(0, smallRadius);

			Action<double, Brush> drawArc = (double AEndPct, Brush AFillBrush) =>
				{
					var smallStart = getArcPoint(AEndPct, smallRadius);
					var bigEnd = getArcPoint(AEndPct, bigRadius);

					bool isBigArc = Math.Abs(PercentToAngle(0) - PercentToAngle(AEndPct)) > Math.PI;
					PathFigure pf = new PathFigure();
					pf.StartPoint = bigStart;
					pf.Segments.Add(new ArcSegment(bigEnd, new Size(bigRadius, bigRadius), 0, isBigArc, SweepDirection.Clockwise, true));
					pf.Segments.Add(new LineSegment(smallStart, true));
					pf.Segments.Add(new ArcSegment(smallEnd, new Size(smallRadius, smallRadius), 0, isBigArc, SweepDirection.Counterclockwise, true));
					pf.Segments.Add(new LineSegment(bigStart, true));
					pf.Freeze();

					PathGeometry g = new PathGeometry();
					g.FillRule = FillRule.Nonzero;
					g.Figures.Add(pf);
					g.Freeze();

					drawingContext.DrawGeometry(AFillBrush, null, g);
				};

			drawArc(1, ValueBackground);
			drawArc(valuePct, ValueForeground);

			var holderRadius = ValueWidth*1.5;
			drawingContext.DrawEllipse((IsFocused)? FocusForeground: ValueForeground, null, getArcPoint(valuePct, middleRadius), holderRadius, holderRadius);

			if (!string.IsNullOrEmpty(Title))
			{
				ft = CreateFormattedText(Title, this.Foreground);
				ft.MaxTextWidth = b.Width;
				ft.Trimming = TextTrimming.CharacterEllipsis;
				ft.MaxTextHeight = textHeight + 2;
				ft.TextAlignment = TextAlignment.Center;
				drawingContext.DrawText(ft, new Point(b.Left, b.Top));
			}

			string vStr;
			if (ValueDigitCount > -1)
				vStr = Value.ToString("F" + ValueDigitCount);
			else
				vStr = string.IsNullOrEmpty(ValueStringFormat)? Value.ToString(): string.Format(ValueStringFormat, Value);

			ft = CreateFormattedText(vStr, (IsFocused)? FocusForeground: FocusForeground);
			ft.MaxTextWidth = 2*smallRadius;
			ft.Trimming = TextTrimming.CharacterEllipsis;
			drawingContext.DrawText(ft, new Point(FCenter.X - ft.Width/2, FCenter.Y - ft.Height/2));

			var pt = getArcPoint(0, bigRadius);
			ft = CreateFormattedText(MinValue.ToString(), Foreground);
			drawingContext.DrawText(ft, new Point(pt.X - ft.Width, pt.Y));

			pt = getArcPoint(1, bigRadius);
			ft = CreateFormattedText(MaxValue.ToString(), Foreground);
			drawingContext.DrawText(ft, pt);
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			InvalidateVisual();
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			InvalidateVisual();
		}

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			"MinValue", typeof(double), typeof(RoundScroller),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender));

		public double MinValue
		{
			get { return (double)GetValue(MinValueProperty); }
			set { SetValue(MinValueProperty, value); }
		}

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			"MaxValue", typeof(double), typeof(RoundScroller),
			new FrameworkPropertyMetadata(1D, FrameworkPropertyMetadataOptions.AffectsRender));

		public double MaxValue
		{
			get { return (double)GetValue(MaxValueProperty); }
			set { SetValue(MaxValueProperty, value); }
		}

		public static readonly DependencyProperty ValueWidthProperty = DependencyProperty.Register(
			"ValueWidth", typeof(double), typeof(RoundScroller),
			new FrameworkPropertyMetadata(5D, FrameworkPropertyMetadataOptions.AffectsRender));

		public double ValueWidth
		{
			get { return (double)GetValue(ValueWidthProperty); }
			set { SetValue(ValueWidthProperty, value); }
		}

		#region ValueDigitCount
		public static readonly DependencyProperty ValueDigitCountProperty = DependencyProperty.Register(
			"ValueDigitCount", typeof(int), typeof(RoundScroller),
			new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

		[Description("Отрицательные значения соответствуют отсутсвию контроля над кол-вом нулей при округлении. Формат вывода задаётся свойством ValueStringFormat")]
		public int ValueDigitCount
		{
			get { return (int)GetValue(ValueDigitCountProperty); }
			set { SetValue(ValueDigitCountProperty, value); }
		}
		#endregion

		#region Value
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(RoundScroller),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender, null, ValueCoerceCallback));

		private static object ValueCoerceCallback(DependencyObject d, object baseValue)
		{
			var rs = d as RoundScroller;
			var v = (double)baseValue;

			if (v > rs.MaxValue)
				v = rs.MaxValue;
			if (v < rs.MinValue)
				v = rs.MinValue;
			if (rs.ValueDigitCount > -1)
				return Math.Round(v, rs.ValueDigitCount);
			return v;
		}

		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
		#endregion

		
		public static readonly DependencyProperty ValueStringFormatProperty = DependencyProperty.Register(
			"ValueStringFormat", typeof(string), typeof(RoundScroller),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

		[Description("Форматирует Value, но не действительно, когда задействован ValueDigitCount")]
		public string ValueStringFormat
		{
			get { return (string)GetValue(ValueStringFormatProperty); }
			set { SetValue(ValueStringFormatProperty, value); }
		}

		public static readonly DependencyProperty ValueForegroundProperty = DependencyProperty.Register(
			"ValueForeground", typeof(Brush), typeof(RoundScroller),
			new FrameworkPropertyMetadata(SystemColors.HighlightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush ValueForeground
		{
			get { return (Brush)GetValue(ValueForegroundProperty); }
			set { SetValue(ValueForegroundProperty, value); }
		}

		public static readonly DependencyProperty ValueBackgroundProperty = DependencyProperty.Register(
			"ValueBackground", typeof(Brush), typeof(RoundScroller),
			new FrameworkPropertyMetadata(SystemColors.ControlLightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush ValueBackground
		{
			get { return (Brush)GetValue(ValueBackgroundProperty); }
			set { SetValue(ValueBackgroundProperty, value); }
		}

		public static readonly DependencyProperty FocusForegroundProperty = DependencyProperty.Register(
			"FocusForeground", typeof(Brush), typeof(RoundScroller),
			new FrameworkPropertyMetadata(SystemColors.ControlLightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush FocusForeground
		{
			get { return (Brush)GetValue(FocusForegroundProperty); }
			set { SetValue(FocusForegroundProperty, value); }
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			"Title", typeof(string), typeof(RoundScroller),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty HasFocusedParentProperty = DependencyProperty.Register(
			"HasFocusedParent", typeof(bool), typeof(RoundScroller),
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
			"ParentIsFocused", typeof(bool), typeof(RoundScroller),
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

		protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
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
				var p = e.GetPosition(this);

				var dx = p.X - FCenter.X;
				var dy = p.Y - FCenter.Y;

				var a = Math.Atan2(dy, dx);
				a -= Math.PI/2;
				if (a < 0)
					a += Math.PI*2;
				a = a*180/Math.PI;
				var minA = FLeftRightTab2;
				var maxA = 360 - FLeftRightTab2;

				if (a < minA)
					a = minA;
				if (a > maxA)
					a = maxA;
				var maxminDelta = MaxValue - MinValue;

				var pct = (a - minA)/(maxA - minA);
				var newValue = MinValue + pct*maxminDelta;

				if (Math.Abs(newValue - Value) < maxminDelta*0.8)
					Value = newValue;

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
			base.OnMouseLeave(e);
			FIsMouseDown = false;
			e.Handled = true;
		}

		protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
		{
			Focus();

			double minDelta;
			if (ValueDigitCount > -1)
				minDelta = Math.Pow(10, -ValueDigitCount);
			else
				minDelta = 1/(2.828*ActualWidth)*(MaxValue - MinValue);

			Value += Math.Sign(e.Delta)*minDelta;

			e.Handled = true;
			base.OnMouseWheel(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;

			double minDelta;
			if (ValueDigitCount > -1)
				minDelta = Math.Pow(10, -ValueDigitCount);
			else
				minDelta = 1/(2.828*ActualWidth)*(MaxValue - MinValue);

			switch (e.Key)
			{
				case Key.Up:
				case Key.Right:
					Value += minDelta;
					break;
				case Key.Down:
				case Key.Left:
					Value -= minDelta;
					break;
				case Key.PageUp:
					Value += 0.1*(MaxValue - MinValue);
					break;
				case Key.PageDown:
					Value -= 0.1*(MaxValue - MinValue);
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
