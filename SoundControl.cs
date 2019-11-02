using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SoundMap
{
	public class SoundControl : Control
	{
		#region struct RenderPoint
		private struct RenderPoint
		{
			private const double Radius = 7;
			private Geometry Region { get; }
			private Point Center { get; }

			public SoundPoint Link { get; }

			public RenderPoint(Point ACenter, SoundPoint ALink)
			{
				Link = ALink;
				Center = ACenter;
				Region = new EllipseGeometry(ACenter, Radius, Radius);
				Region.Freeze();
			}

			public void DrawTo(DrawingContext drawingContext)
			{
				drawingContext.DrawGeometry(SystemColors.WindowTextBrush, null, Region);
				if (Link.IsSelected)
					drawingContext.DrawEllipse(null, new Pen(SystemColors.HighlightBrush, 2), Center, Radius + 2, Radius + 2);
			}

			public bool Contain(Point APoint)
			{
				return Region.FillContains(APoint);
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
		private RenderPoint? FHoldPoint = null;
		private Point FDownPoint;
		private Point FDownRelative;
		private HVStatus FHVControl = HVStatus.Off;
		private readonly Pen FHVPen;

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

		public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
			"Points", typeof(SoundPointCollection), typeof(SoundControl),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, PointsCollectionChanged));

		private static void PointsCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ptc = (d as SoundControl);

			if (e.OldValue != null)
			{
				var coll = (INotifyCollectionChanged)e.OldValue;
				coll.CollectionChanged -= ptc.Points_CollectionChanged;
			}

			if (e.NewValue != null)
			{
				var coll = (INotifyCollectionChanged)e.NewValue;
				coll.CollectionChanged += ptc.Points_CollectionChanged;
			}
		}

		public SoundPointCollection Points
		{
			get => (SoundPointCollection)GetValue(PointsProperty);
			set => SetValue(PointsProperty, value);
		}

		private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();
		}

		public static readonly DependencyProperty AddPointEventProperty = DependencyProperty.Register(
			"AddPointEvent", typeof(Action<SoundPoint>), typeof(SoundControl));

		public Action<SoundPoint> AddPointEvent
		{
			get => (Action<SoundPoint>)GetValue(AddPointEventProperty);
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

			if (Points != null)
				foreach (var p in Points)
				{
					var c = new Point((int)(p.Relative.X * ActualWidth), (int)(p.Relative.Y * ActualHeight));

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
		}

		protected SoundPoint FirstSelectedPoint
		{
			get
			{
				return Points.FirstOrDefault(sp => sp.IsSelected);
			}
			set
			{
				foreach (var pts in Points)
					pts.IsSelected = pts == value;
			}
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

		private RenderPoint? GetPointAtMouse(Point APoint)
		{
			foreach (var rp in FRenderPoints)
				if (rp.Contain(APoint))
					return rp;
			return null;
		}

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			var p = e.GetPosition(this);

			RenderPoint? toDelete = GetPointAtMouse(p);

			try
			{
				if (toDelete.HasValue && (DeletePointEvent != null))
				{
					DeletePointEvent.Invoke(toDelete.Value.Link);
					return;
				}

				if (AddPointEvent != null)
				{
					var newPoint = new SoundPoint(p.X / ActualWidth, p.Y / ActualHeight);
					AddPointEvent.Invoke(newPoint);
					FirstSelectedPoint = newPoint;
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
				FHoldPoint = GetPointAtMouse(p);
				if (FHoldPoint.HasValue)
				{
					FDownPoint = p;
					FDownRelative = FHoldPoint.Value.Link.Relative;

					FirstSelectedPoint = FHoldPoint.Value.Link;
				}
			}

			Keyboard.Focus(this);
			e.Handled = true;
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (FHoldPoint.HasValue)
			{
				var p = e.GetPosition(this);
				var pixelOffset = p - FDownPoint;

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
				FHoldPoint.Value.Link.Relative = Point.Add(FDownRelative, relativeOffset);
				InvalidateVisual();
			}

			e.Handled = true;
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Released)
				FHoldPoint = null;
			base.OnMouseUp(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (((e.Key == Key.LeftShift) || (e.Key == Key.RightShift)) && !e.IsRepeat)
			{
				FHVControl = HVStatus.Undefined;
				InvalidateVisual();
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
