using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SoundMap.Controls
{
	public class SoundControl : Control
	{
		#region class RenderPoint
		private class RenderPoint
		{
			private const double Radius = 7;
			private Geometry Region { get; }
			private Point Center { get; }
			private Rect Bounds { get; }

			private static readonly SolidColorBrush MuteTextBrush = CreateCombineBrush(Colors.Yellow, SystemColors.WindowTextColor, 0.75f);
			private static readonly SolidColorBrush SoloTextBrush = CreateCombineBrush(Colors.Red, SystemColors.WindowTextColor, 0.75f);

			public SoundPoint Link { get; }

			private static SolidColorBrush CreateCombineBrush(Color AAColor, Color ABColor, float ABPrecent)
			{
				var cl = Color.Add(Color.Multiply(AAColor, 1 - ABPrecent), Color.Multiply(ABColor, ABPrecent));
				return new SolidColorBrush(cl);
			}

			public RenderPoint(Point ACenter, SoundPoint ALink)
			{
				Link = ALink;
				Center = ACenter;
				Region = new EllipseGeometry(ACenter, Radius, Radius);
				Region.Freeze();
				Bounds = new Rect(ACenter.X - Radius, ACenter.Y - Radius, Radius * 2, Radius * 2);
			}

			public void DrawTo(DrawingContext drawingContext)
			{
				drawingContext.DrawGeometry(SystemColors.WindowTextBrush, null, Region);
				if (Link.IsSelected)
					drawingContext.DrawEllipse(null, new Pen(SystemColors.HighlightBrush, 2), Center, Radius + 2, Radius + 2);

				var tf = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

				Point stp = new Point(Center.X + Radius, Center.Y + Radius);

				if (Link.IsMute)
				{
					FormattedText t = new FormattedText("M", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 12, MuteTextBrush);
					drawingContext.DrawText(t, stp);
					stp.X += t.Width;
				}

				if (Link.IsSolo)
				{
					FormattedText t = new FormattedText("S", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 12, SoloTextBrush);
					drawingContext.DrawText(t, stp);
				}
			}

			public bool Contain(Point APoint)
			{
				return Region.FillContains(APoint);
			}

			public bool Inside(Rect ARect)
			{
				return !Rect.Intersect(ARect, Bounds).IsEmpty;
			}
		}
		#endregion

		#region enum HVStatus
		private enum HVStatus
		{
			Off,
			Undefined,
			Horizontal,
			Vertical
		}
		#endregion

		private readonly List<RenderPoint> FRenderPoints = new List<RenderPoint>();
		private bool FIsMoveMode = false;
		private Point FDownPoint;
		private HVStatus FHVControl = HVStatus.Off;
		private readonly Pen FHVPen;
		private Rect FSelectedRect = Rect.Empty;

		public SoundControl()
		{
			ClipToBounds = true;
			SnapsToDevicePixels = true;
			Focusable = true;

			FHVPen = new Pen(SystemColors.ControlDarkBrush, 1);
			FHVPen.DashStyle = new DashStyle(new double[] { 1, 3 }, 0);
			FHVPen.Freeze();
		}

		#region Properties

		public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
			"Project", typeof(SoundProject), typeof(SoundControl),
			new FrameworkPropertyMetadata());

		public SoundProject Project
		{
			get => (SoundProject)GetValue(ProjectProperty);
			set => SetValue(ProjectProperty, value);
		}

		public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
			"Points", typeof(SoundPointCollection), typeof(SoundControl),
			new FrameworkPropertyMetadata(new SoundPointCollection(), PointsCollectionChanged));

		public SoundPointCollection Points
		{
			get => (SoundPointCollection)GetValue(PointsProperty);
			set => SetValue(PointsProperty, value);
		}

		private static void PointsCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ptc = (d as SoundControl);

			if (e.OldValue != null)
			{
				var spc = (SoundPointCollection)e.OldValue;
				spc.CollectionChanged -= ptc.Points_CollectionChanged;
				spc.PointPropertyChanged -= ptc.Points_PointPropertyChanged;
			}

			if (e.NewValue != null)
			{
				var spc = (SoundPointCollection)e.NewValue;
				spc.CollectionChanged += ptc.Points_CollectionChanged;
				spc.PointPropertyChanged += ptc.Points_PointPropertyChanged;
			}
		}

		private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();
		}

		private void Points_PointPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			InvalidateVisual();
		}

		public static readonly DependencyProperty SelectedPointsProperty = DependencyProperty.Register(
			"SelectedPoints", typeof(SoundPointCollection), typeof(SoundControl),
			new FrameworkPropertyMetadata(new SoundPointCollection(), FrameworkPropertyMetadataOptions.AffectsRender, null));

		public SoundPointCollection SelectedPoints
		{
			get => (SoundPointCollection)GetValue(SelectedPointsProperty);
			set => SetValue(SelectedPointsProperty, value);
		}

		public static readonly DependencyProperty AddPointEventProperty = DependencyProperty.Register(
			"AddPointEvent", typeof(Action<SoundPoint>), typeof(SoundControl));

		public Action<Point> AddPointEvent
		{
			get => (Action<Point>)GetValue(AddPointEventProperty);
			set => SetValue(AddPointEventProperty, value);
		}

		public static readonly DependencyProperty DeletePointEventProperty = DependencyProperty.Register(
			"DeletePointEvent", typeof(Action<SoundPoint>), typeof(SoundControl));

		public Action<SoundPoint> DeletePointEvent
		{
			get => (Action<SoundPoint>)GetValue(DeletePointEventProperty);
			set => SetValue(DeletePointEventProperty, value);
		}

		#endregion

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			var actualBounds = new Rect(0, 0, ActualWidth, ActualHeight);

			if (IsFocused)
				drawingContext.DrawRectangle(SystemColors.WindowBrush, new Pen(SystemColors.ControlDarkBrush, 2), actualBounds);
			else
				drawingContext.DrawRectangle(SystemColors.WindowBrush, null, actualBounds);

			FRenderPoints.Clear();

			if ((Points != null) && (Project != null))
				foreach (var p in Points)
				{
					var c = Project.GetPointXY(p, ActualWidth, ActualHeight);

					if ((p.IsSelected) && (FHVControl != HVStatus.Off))
					{
						if (FHVControl != HVStatus.Horizontal)
							drawingContext.DrawLine(FHVPen, new Point(c.X, actualBounds.Top), new Point(c.X, actualBounds.Bottom));
						if (FHVControl != HVStatus.Vertical)
							drawingContext.DrawLine(FHVPen, new Point(actualBounds.Left, c.Y), new Point(actualBounds.Right, c.Y));
					}

					var rp = new RenderPoint(c, p);
					rp.DrawTo(drawingContext);
					FRenderPoints.Add(rp);
				}

			drawingContext.DrawRectangle(null, FHVPen, FSelectedRect);
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

		private RenderPoint GetPointAtMouse(Point APoint)
		{
			foreach (var rp in FRenderPoints)
				if (rp.Contain(APoint))
					return rp;
			return null;
		}

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			var p = e.GetPosition(this);

			RenderPoint toDelete = GetPointAtMouse(p);

			try
			{
				if ((toDelete != null) && (DeletePointEvent != null))
				{
					DeletePointEvent.Invoke(toDelete.Link);
					return;
				}

				if (AddPointEvent != null)
				{
					AddPointEvent.Invoke(p.X / ActualWidth, p.Y / ActualHeight);
					foreach (var pts in Points)
						pts.IsSelected = pts == newPoint;
				}
			}
			finally
			{
				e.Handled = true;
				base.OnMouseDoubleClick(e);
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var p = e.GetPosition(this);
				FDownPoint = p;
				FSelectedRect = Rect.Empty;

				var hp = GetPointAtMouse(p);
				if (hp == null)
				{
					// Если жмётся пустое место, то снять все выделения
					Points.ChangedLock();
					foreach (var pts in Points)
						pts.IsSelected = false;
					Points.ChangedUnlock();
				}
				else
				{
					// Если выбирается невыделенный, то сбросить выделения других
					if (!hp.Link.IsSelected)
					{
						Points.ChangedLock();
						foreach (var pts in Points)
							pts.IsSelected = pts == hp.Link;
						Points.ChangedUnlock();
					}

					foreach (var pts in SelectedPoints)
						pts.StartRelative = pts.Relative;

					FIsMoveMode = true;
				}

				InvalidateVisual();
			}

			Keyboard.Focus(this);
			e.Handled = true;
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var p = e.GetPosition(this);
			var pixelOffset = p - FDownPoint;

			if (FIsMoveMode)
			{
				if (FHVControl != HVStatus.Off)
				{
					if (FHVControl == HVStatus.Undefined)
					{
						if (Math.Abs(pixelOffset.X) > Math.Abs(pixelOffset.Y))
							FHVControl = HVStatus.Horizontal;
						else
							FHVControl = HVStatus.Vertical;
					}

					if (FHVControl == HVStatus.Horizontal)
						pixelOffset.Y = 0;
					else
						pixelOffset.X = 0;
				}

				var relativeOffset = new Vector(pixelOffset.X / ActualWidth, pixelOffset.Y / ActualHeight);

				Points.ChangedLock();
				foreach (var pts in SelectedPoints)
					pts.Relative = Point.Add(pts.StartRelative, relativeOffset);
				Points.ChangedUnlock(true);

				InvalidateVisual();
			}
			else
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var rl = FDownPoint.X;
					var rt = FDownPoint.Y;
					var rw = pixelOffset.X;
					var rh = pixelOffset.Y;

					if (rw < 0)
					{
						rl += rw;
						rw = -rw;
					}

					if (rh < 0)
					{
						rt += rh;
						rh = -rh;
					}

					FSelectedRect = new Rect(rl, rt, rw, rh);
					InvalidateVisual();
				}
			}

			e.Handled = true;
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			//OnMouseUp(new MouseButtonEventArgs(e.Device, e.Timestamp, e.MouseDevice.M)
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Released)
			{
				FIsMoveMode = false;
				if (!FSelectedRect.IsEmpty)
				{
					Points.ChangedLock();
					foreach (var rp in FRenderPoints)
						rp.Link.IsSelected = rp.Inside(FSelectedRect);
					Points.ChangedUnlock();

					FSelectedRect = Rect.Empty;
					InvalidateVisual();
				}
			}
			base.OnMouseUp(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.LeftShift:
				case Key.RightShift:
					if (!e.IsRepeat)
					{
						e.Handled = true;
						FHVControl = HVStatus.Undefined;
						InvalidateVisual();
					}
					break;
				case Key.Left:
					Points.ChangedLock();
					foreach (var p in SelectedPoints)
						p.Relative = new Point(p.Relative.X - 1 / ActualWidth, p.Relative.Y);
					Points.ChangedUnlock();
					e.Handled = true;
					break;
				case Key.Right:
					Points.ChangedLock();
					foreach (var p in SelectedPoints)
						p.Relative = new Point(p.Relative.X + 1 / ActualWidth, p.Relative.Y);
					Points.ChangedUnlock();
					e.Handled = true;
					break;
				case Key.Up:
					Points.ChangedLock();
					foreach (var p in SelectedPoints)
						p.Relative = new Point(p.Relative.X, p.Relative.Y - 1 / ActualHeight);
					Points.ChangedUnlock();
					e.Handled = true;
					break;
				case Key.Down:
					Points.ChangedLock();
					foreach (var p in SelectedPoints)
						p.Relative = new Point(p.Relative.X, p.Relative.Y + 1 / ActualHeight);
					Points.ChangedUnlock();
					e.Handled = true;
					break;
				case Key.S:
					if (SelectedPoints.Count == 1)
						SelectedPoints[0].IsSolo = !SelectedPoints[0].IsSolo;
					e.Handled = true;
					break;
				case Key.M:
					Points.ChangedLock();
					foreach (var p in SelectedPoints)
						p.IsMute = !p.IsMute;
					Points.ChangedUnlock();
					e.Handled = true;
					break;
			}

			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if ((e.Key == Key.LeftShift) || (e.Key == Key.RightShift))
			{
				FHVControl = HVStatus.Off;
				InvalidateVisual();
			}

			base.OnKeyUp(e);
		}
	}
}
