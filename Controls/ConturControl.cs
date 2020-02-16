using Interpolators;
using SoundMap.Waveforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SoundMap.Controls
{
	public class ConturControl : Control
	{
		private const int MaxHistoryCount = 4;

		private bool FIsMouseDown = false;
		private Point FOldR;
		private Point FMoveDownPoint;
		private readonly Queue<Point[]> FHistory = new Queue<Point[]>();

		private RelayCommand FMedianaFilterCommand = null;
		private RelayCommand FNormalizeFilterCommand = null;
		private RelayCommand FResetFilterCommand = null;
		private RelayCommand FHalfOffsetFilterCommand = null;
		private RelayCommand FBackFilterCommand = null;
		private RelayCommand FSineGenerateFilterCommand = null;

		public ConturControl()
			: base()
		{
		}

		private void ApplyFilter(Point[] newPoints, bool addToHistory = true)
		{
			if (addToHistory)
			{
				FHistory.Enqueue(OneHerz.ToArray());
				if (FHistory.Count > MaxHistoryCount)
					FHistory.Dequeue();
			}
			OneHerz.Clear();
			OneHerz.AddRange(newPoints);
			OnPropertyChanged(new DependencyPropertyChangedEventArgs(OneHerzProperty, OneHerz, OneHerz));
			InvalidateVisual();
			CommandManager.InvalidateRequerySuggested();
		}

		public RelayCommand MedianaFilterCommand
		{
			get
			{
				if (FMedianaFilterCommand == null)
					FMedianaFilterCommand = new RelayCommand((param) =>
					{
						Point[] r;
						if (param == null)
							r = OneHerz.MedianaFilter();
						else
							r = OneHerz.MedianaFilter(Convert.ToInt32(param));
						ApplyFilter(r);
					});
				return FMedianaFilterCommand;
			}
		}

		public RelayCommand ResetFilterCommand
		{
			get
			{
				if (FResetFilterCommand == null)
					FResetFilterCommand = new RelayCommand((p) => ApplyFilter(OneHerz.ResetFilter()));
				return FResetFilterCommand;
			}
		}

		public RelayCommand NormalizeFilterCommand
		{
			get
			{
				if (FNormalizeFilterCommand == null)
					FNormalizeFilterCommand = new RelayCommand((p) => ApplyFilter(OneHerz.NormalizeFilter()));
				return FNormalizeFilterCommand;
			}
		}

		public RelayCommand HalfOffsetFilterCommand
		{
			get
			{
				if (FHalfOffsetFilterCommand == null)
					FHalfOffsetFilterCommand = new RelayCommand((p) => ApplyFilter(OneHerz.HalfOffsetFilter()));
				return FHalfOffsetFilterCommand;
			}
		}

		public RelayCommand BackFilterCommand
		{
			get
			{
				if (FBackFilterCommand == null)
					FBackFilterCommand = new RelayCommand(
						(p) =>
						{
							var r = BackFilter();
							if (r != null)
								ApplyFilter(r, false);
						},
						(e) => FHistory.Count > 0);
				return FBackFilterCommand;
			}
		}

		private Point[] BackFilter()
		{
			if (FHistory.Count > 0)
				return FHistory.Dequeue();
			return null;
		}

		public RelayCommand SineGenerateFilterCommand
		{
			get
			{
				if (FSineGenerateFilterCommand == null)
					FSineGenerateFilterCommand = new RelayCommand((par) =>
					{
						ApplyFilter(OneHerz.SineGenerate(ActualWidth));
					});
				return FSineGenerateFilterCommand;
			}
		}

		#region WaveformValuesProperty

		public static readonly DependencyProperty OneHerzProperty = DependencyProperty.Register(
			"OneHerz", typeof(OneHerzList), typeof(ConturControl), new FrameworkPropertyMetadata(new OneHerzList(), OneHerzChanged));

		private static void OneHerzChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as ConturControl).InvalidateVisual();
		}


		public OneHerzList OneHerz
		{
			get => (OneHerzList)GetValue(OneHerzProperty);
			set => SetValue(OneHerzProperty, value);
		}

		#endregion

		#region LinePenProperty

		public static readonly DependencyProperty LinePenProperty = DependencyProperty.Register(
			"LinePen", typeof(Pen), typeof(ConturControl), new FrameworkPropertyMetadata(new Pen(Brushes.DarkOrange, 1), FrameworkPropertyMetadataOptions.AffectsRender));

		public Pen LinePen
		{
			get => (Pen)GetValue(LinePenProperty);
			set => SetValue(LinePenProperty, value);
		}

		#endregion

		#region PointFillProperty
		public static readonly DependencyProperty PointFillProperty = DependencyProperty.Register(
			"PointFill", typeof(Brush), typeof(ConturControl), new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

		public Brush PointFill
		{
			get => (Brush)GetValue(PointFillProperty);
			set => SetValue(PointFillProperty, value);
		}
		#endregion

		#region DrawContur

		public static readonly DependencyProperty DrawConturProperty = DependencyProperty.Register(
			"DrawContur", typeof(bool), typeof(ConturControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

		public bool DrawContur
		{
			get => (bool)GetValue(DrawConturProperty);
			set => SetValue(DrawConturProperty, value);
		}

		#endregion

		protected override void OnRender(DrawingContext drawingContext)
		{
			const int RectSz2 = 1;
			base.OnRender(drawingContext);

			drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));

			if (DrawContur)
			{
				var rs = OneHerz.Resample(0, ActualWidth, 0, ActualHeight, (int)ActualWidth);

				for (int i = 1; i < rs.Length; i++)
					drawingContext.DrawLine(LinePen, rs[i - 1], rs[i]);
			}

			for (int i = 0; i < OneHerz.Count; i++)
			{
				var ap = new Point(OneHerz[i].X * ActualWidth, OneHerz[i].Y * ActualHeight);
				drawingContext.DrawRectangle(PointFill, null, new Rect(ap.X - RectSz2, ap.Y - RectSz2, 2 * RectSz2, 2 * RectSz2));
			}
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			InvalidateVisual();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			FIsMouseDown = true;
			FMoveDownPoint = e.GetPosition(this);
			FOldR = new Point(FMoveDownPoint.X / ActualWidth, FMoveDownPoint.Y / ActualHeight);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (FIsMouseDown)
			{
				Point newP = e.GetPosition(this);

				// Расчёты ведутся в относительных координатах
				var newR = new Point(newP.X / ActualWidth, newP.Y / ActualHeight);

				List<Point> newPoints = new List<Point>(OneHerz.Count);

				if (newR.X < FOldR.X)
					for (int i = 0; i < OneHerz.Count; i++)
					{
						if (OneHerz[i].X < newR.X)
							newPoints.Add(OneHerz[i]);
						else if (OneHerz[i].X > FOldR.X)
							newPoints.Add(OneHerz[i]);
					}
				else
					for (int i = 0; i < OneHerz.Count; i++)
					{
						if (OneHerz[i].X > newR.X)
							newPoints.Add(OneHerz[i]);
						else if (OneHerz[i].X < FOldR.X)
							newPoints.Add(OneHerz[i]);
					}

				newPoints.Add(newR);
				newPoints.Add(FOldR);
				newPoints.Sort((a, b) => a.X.CompareTo(b.X));

				OneHerz.Clear();
				OneHerz.AddRange(newPoints);

				FOldR = newR;
				e.Handled = true;

				InvalidateVisual();
			}
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			FIsMouseDown = false;

			var moveUpPoint = e.GetPosition(this);
			if (FMoveDownPoint == moveUpPoint)
			{
				OneHerz.Add(FOldR);
				OneHerz.Sort((a, b) => a.X.CompareTo(b.X));
				InvalidateVisual();
			}
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			FIsMouseDown = false;
		}
	}
}
