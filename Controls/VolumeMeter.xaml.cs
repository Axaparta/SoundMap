using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DeepSound.Controls
{
	public partial class VolumeMeter: UserControl
	{
		private const int CellDelta = 1;

		private Rectangle[] FLeftGridRectangles = null;
		private Rectangle[] FRightGridRectangles = null;
		private GradientStop[] FSortGaudeGradient = null;

		public VolumeMeter()
		{
			InitializeComponent();
      SizeChanged += (s, e) => RecreateGridCells();
		}

		public static readonly DependencyProperty CellSizeProperty = DependencyProperty.Register(
			"CellSize", typeof(int), typeof(VolumeMeter),
			new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender, StaticRecreateGridCells)
		);

		public int CellSize
		{
			get { return (int)GetValue(CellSizeProperty); }
			set { SetValue(CellSizeProperty, value); }
		}

		public static readonly DependencyProperty GaudeGradientProperty = DependencyProperty.Register(
			"GaudeGradient", typeof(GradientStopCollection), typeof(VolumeMeter),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, StaticSetGaudeGradient)
		);

		public GradientStopCollection GaudeGradient
		{
			get { return (GradientStopCollection)GetValue(GaudeGradientProperty); }
			set { SetValue(GaudeGradientProperty, value); }
		}

		public static readonly DependencyProperty LeftVolumeProperty = DependencyProperty.Register(
			"LeftVolume", typeof(double), typeof(VolumeMeter),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender, StaticVolumeUpdate)
		);

		public double LeftVolume
		{
			get { return (double)GetValue(LeftVolumeProperty); }
			set { SetValue(LeftVolumeProperty, value); }
		}

		public static readonly DependencyProperty RightVolumeProperty = DependencyProperty.Register(
			"RightVolume", typeof(double), typeof(VolumeMeter),
			new FrameworkPropertyMetadata(0D, FrameworkPropertyMetadataOptions.AffectsRender, StaticVolumeUpdate));

		public double RightVolume
		{
			get { return (double)GetValue(RightVolumeProperty); }
			set { SetValue(RightVolumeProperty, value); }
		}

		public static readonly DependencyProperty CellInactiveBrushProperty = DependencyProperty.Register(
			"CellInactiveBrush", typeof(Brush), typeof(VolumeMeter),
			new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, StaticVolumeUpdate));

		public Brush CellInactiveBrush
		{
			get { return (Brush)GetValue(CellInactiveBrushProperty); }
			set { SetValue(CellInactiveBrushProperty, value); }
		}

		private static void StaticRecreateGridCells(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as VolumeMeter).RecreateGridCells();
		}

		private static void StaticSetGaudeGradient(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as VolumeMeter).SetSortGaudeGradient((GradientStopCollection)e.NewValue);
		}

		private static void StaticVolumeUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as VolumeMeter).VolumeUpdate();
		}

		private void VolumeUpdate()
		{
			if ((FLeftGridRectangles == null) || (FRightGridRectangles == null))
				return;

			Action<double, Rectangle[]> update = (double AVolume, Rectangle[] ARectangles) =>
				{
					Brush b = null;
					if (AVolume > 1)
						b = (Brush)ARectangles.Last().Tag;
					if (AVolume < 0)
						b = (Brush)ARectangles.First().Tag;

					if (b == null)
					{
						int ind = (int)Math.Ceiling(ARectangles.Length*AVolume);
						for (int i = 0; i < ARectangles.Length; i++)
							if (i >= ind)
								ARectangles[i].Fill = CellInactiveBrush;
							else
								ARectangles[i].Fill = (Brush)ARectangles[i].Tag;
					}
					else
						for (int i = 0; i < ARectangles.Length; i++)
							ARectangles[i].Fill = b;
				};

			update(LeftVolume, FLeftGridRectangles);
			update(RightVolume, FRightGridRectangles);
		}

		private Brush GetCellFillBrush(double APct)
		{
			if ((FSortGaudeGradient == null) || (FSortGaudeGradient.Length == 0))
				return SystemColors.HighlightBrush;
			Color cl = Colors.Transparent;
			for (int i = 1; i < FSortGaudeGradient.Length; i++)
			{
				if (FSortGaudeGradient[i].Offset == APct)
				{
					cl = FSortGaudeGradient[i].Color;
					break;
				}
				if ((FSortGaudeGradient[i - 1].Offset <= APct) && ( FSortGaudeGradient[i].Offset > APct))
				{
					double delta = FSortGaudeGradient[i].Offset - FSortGaudeGradient[i - 1].Offset;
					float cl1pct = (float)(1 - (APct - FSortGaudeGradient[i - 1].Offset)/delta);
					float cl2pct = (float)(1 - (FSortGaudeGradient[i].Offset - APct)/delta);

					Color cl1 = Color.Multiply(FSortGaudeGradient[i - 1].Color, cl1pct);
					Color cl2 = Color.Multiply(FSortGaudeGradient[i].Color, cl2pct);
					cl = Color.Add(cl1, cl2);
					break;
				}
			}
			var b = new SolidColorBrush(cl);
			b.Freeze();
			return b;
		}

		private void RecreateGridCells()
		{
			RecreateRects(LeftCanvas, ref FLeftGridRectangles);
			RecreateRects(RightCanvas, ref FRightGridRectangles);

			VolumeUpdate();
		}

		private void RecreateRects(Canvas ACanvas, ref Rectangle[] ARectangles)
		{
			ACanvas.Children.Clear();

			if (CellSize < 1)
				return;

			int size = CellSize;
      int cellOffset = size + CellDelta;
      int cellCount = (int)Math.Truncate(ACanvas.ActualHeight/cellOffset);
			int topOffset = (int)ACanvas.ActualHeight - cellCount*cellOffset;
			if (topOffset > cellCount)
			{
				size += topOffset/cellCount;
				cellOffset = size + CellDelta;
				topOffset = (int)ACanvas.ActualHeight - cellCount*cellOffset;
			}
			topOffset /= 2;

			ARectangles = new Rectangle[cellCount];
			for (int i = 0; i < cellCount; i++)
			{
				var r = new Rectangle();
				r.Tag = GetCellFillBrush(1 - (double)i/(cellCount - 1));
				r.SnapsToDevicePixels = true;
				r.Width = ACanvas.ActualWidth - 2*CellDelta;
				r.Height = size;
				Canvas.SetLeft(r, CellDelta);
				Canvas.SetTop(r, topOffset + i*cellOffset);
        ACanvas.Children.Add(r);
				ARectangles[cellCount - i - 1] = r;
			}
		}

		private void SetSortGaudeGradient(GradientStopCollection AUnsortCollection)
		{
			if (AUnsortCollection != null)
			{
				FSortGaudeGradient = AUnsortCollection.OrderBy(gs => gs.Offset).ToArray();
				RecreateGridCells();
			}
		}
	}
}
