using System;
using System.Windows;

namespace SoundMap.Settings
{
	[Serializable]
	public class WindowSettings
	{
		public double? Top { get; set; }
		public double Left { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public bool IsMaximized { get; set; }

		public WindowSettings()
			: base()
		{
			Top = null;
			IsMaximized = false;
		}

		public void ReadFrom(Window AObject)
		{
			IsMaximized = AObject.WindowState == WindowState.Maximized;
			if (!IsMaximized)
			{
				Top = AObject.Top;
				Left = AObject.Left;
				Width = AObject.Width;
				Height = AObject.Height;
			}
		}

		public void ApplyTo(Window AObject)
		{
			if (IsMaximized)
				AObject.WindowState = WindowState.Maximized;
			else
				if (Top.HasValue)
				{
					AObject.Top = Top.Value;
					AObject.Left = Left;
					AObject.Width = Width;
					AObject.Height = Height;
				}
		}
	}
}
