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
		private readonly Typeface FTypeface;
		private Rect FSelectedRect = Rect.Empty;

		public SoundControl()
		{
			ClipToBounds = true;
			SnapsToDevicePixels = true;
			Focusable = true;

			FHVPen = new Pen(SystemColors.ControlDarkBrush, 1);
			FHVPen.DashStyle = new DashStyle(new double[] { 1, 3 }, 0);
			FHVPen.Freeze();

			FTypeface = new Typeface("Consolas");
		}

		#region Properties

		public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
			"Project", typeof(SoundProject), typeof(SoundControl),
			new FrameworkPropertyMetadata(ProjectPropertyChanged));

		public SoundProject Project
		{
			get => (SoundProject)GetValue(ProjectProperty);
			set => SetValue(ProjectProperty, value);
		}

		private static void ProjectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SoundControl sc = (SoundControl)d;

			SoundProject p;
			p = e.OldValue as SoundProject;
			if (p != null)
			{
				p.PropertyChanged -= sc.Points_PointPropertyChanged;
				p.Points.CollectionChanged -= sc.Points_CollectionChanged;
				p.Points.PointPropertyChanged -= sc.Points_PointPropertyChanged;
			}

			p = e.NewValue as SoundProject;

			if (p != null)
			{
				p.PropertyChanged += sc.Points_PointPropertyChanged;
				p.Points.CollectionChanged += sc.Points_CollectionChanged;
				p.Points.PointPropertyChanged += sc.Points_PointPropertyChanged;
			}

			sc.InvalidateVisual();
		}

		private void Points_PointPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			InvalidateVisual();
		}

		private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();
		}


		#endregion

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			var actualBounds = new Rect(0, 0, ActualWidth, ActualHeight);

			if (IsFocused)
				drawingContext.DrawRectangle(Background, new Pen(SystemColors.ControlDarkBrush, 2), actualBounds);
			else
				drawingContext.DrawRectangle(Background, null, actualBounds);

			FRenderPoints.Clear();

			if (Project != null)
			{
				foreach (var p in Project.Points)
				{
					var c = GetPointXY(p);

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

				FormattedText ft = new FormattedText(Project.Settings.MinFrequency.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, FTypeface, 12, Foreground);
				drawingContext.DrawText(ft, new Point(0, ActualHeight - ft.Height));

				ft = new FormattedText(Project.Settings.MaxFrequency.ToString() + " Hz", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, FTypeface, 12, Foreground);
				drawingContext.DrawText(ft, new Point(ActualWidth - ft.Width, ActualHeight - ft.Height));
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
			if (Project == null)
				return;

			var p = e.GetPosition(this);
			RenderPoint toDelete = GetPointAtMouse(p);

			try
			{
				if (toDelete == null)
				{
					var newPoint = new SoundPoint()
					{
						Frequency = GetFrequency(p),
						Volume = GetVolume(p)
					};
					Project.Points.Add(newPoint);
					foreach (var pts in Project.Points)
						pts.IsSelected = pts == newPoint;
				}
				else
					Project.Points.Remove(toDelete.Link);
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

				if (Project == null)
					return;

				var pts = Project.Points;

				var hp = GetPointAtMouse(p);
				if (hp == null)
				{
					// Если жмётся пустое место, то снять все выделения
					pts.ChangedLock();
					foreach (var sp in pts)
						sp.IsSelected = false;
					pts.ChangedUnlock();
				}
				else
				{
					// Если выбирается невыделенный, то сбросить выделения других
					if (!hp.Link.IsSelected)
					{
						pts.ChangedLock();
						foreach (var sp in pts)
							sp.IsSelected = sp == hp.Link;
						pts.ChangedUnlock();
					}

					foreach (var sp in Project.SelectedPoints)
						sp.Start = sp.Clone();

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
			if (Project == null)
				return;

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

				var pts = Project.SelectedPoints;
				pts.ChangedLock();
				for (int i = 0; i < pts.Count; i++)
				{
					var sp = pts[i];
					SoundPointOffset(sp, sp.Start, pixelOffset);
				}
				pts.ChangedUnlock(true);

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

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (Project == null)
				return;

			if (e.LeftButton == MouseButtonState.Released)
			{
				FIsMoveMode = false;
				if (!FSelectedRect.IsEmpty)
				{
					Project.Points.ChangedLock();
					foreach (var rp in FRenderPoints)
						rp.Link.IsSelected = rp.Inside(FSelectedRect);
					Project.Points.ChangedUnlock();

					FSelectedRect = Rect.Empty;
					InvalidateVisual();
				}
			}
			base.OnMouseUp(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Project == null)
				return;

			Project.Points.ChangedLock();

			void Offset(int dX, int dY)
			{
				var v = new Vector(dX, dY);
				for (int i = 0; i < Project.SelectedPoints.Count; i++)
				{
					var sp = Project.SelectedPoints[i];
					SoundPointOffset(sp, sp, v);
				}
				e.Handled = true;
			}

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
					Offset(-1, 0);
					break;
				case Key.Right:
					Offset(1, 0);
					break;
				case Key.Up:
					Offset(0, -1);
					break;
				case Key.Down:
					Offset(0, +1);
					break;
				case Key.S:
					if (Project.SelectedPoints.Count == 1)
						Project.SelectedPoints[0].IsSolo = !Project.SelectedPoints[0].IsSolo;
					e.Handled = true;
					break;
				case Key.M:
					for (int i = 0; i < Project.SelectedPoints.Count; i++)
						Project.SelectedPoints[i].IsMute = !Project.SelectedPoints[i].IsMute;
					e.Handled = true;
					break;
			}

			Project.Points.ChangedUnlock();

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

		public Point GetPointXY(SoundPoint APoint)
		{
			double FLogFMin = Math.Log(Project.Settings.MinFrequency, 2);
			double FLogFMax = Math.Log(Project.Settings.MaxFrequency, 2);
			double pow = Math.Log(APoint.Frequency, 2);
			double rx = (pow - FLogFMin) / (FLogFMax - FLogFMin);

			var p = new Point(ActualWidth * rx, ActualHeight * (1 - APoint.Volume));

			if (p.X < 0)
				p.X = 0;
			if (p.Y < 0)
				p.Y = 0;
			if (p.X > ActualWidth)
				p.X = ActualWidth;
			if (p.Y > ActualHeight)
				p.Y = ActualHeight;

			return p;
		}

		private double GetFrequency(Point APoint)
		{
			double FLogFMin = Math.Log(Project.Settings.MinFrequency, 2);
			double FLogFMax = Math.Log(Project.Settings.MaxFrequency, 2);
			var pow = FLogFMin + (FLogFMax - FLogFMin) * APoint.X / ActualWidth;
			return Math.Pow(2, pow);
		}

		private double GetVolume(Point APoint)
		{
			return 1 - APoint.Y / ActualHeight;
		}

		private void SoundPointOffset(SoundPoint APoint, SoundPoint AStartPoint, Vector AOffset)
		{
			// Прежняя позиция точки
			var spXY = GetPointXY(AStartPoint);
			// Новая позиция
			spXY += AOffset;
			// Новая частота и громкость
			APoint.Frequency = GetFrequency(spXY);
			APoint.Volume = GetVolume(spXY);
		}
	}
}
