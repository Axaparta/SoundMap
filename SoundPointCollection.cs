﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SoundMap
{
	public class SoundPointCollection : ObservableCollection<SoundPoint>
	{
		private int FChangedLock = 0;
		private bool FChangedNeed = false;

		public event PropertyChangedEventHandler PointPropertyChanged;

		public SoundPointCollection()
		{
		}

		public SoundPointCollection(List<SoundPoint> list) : base(list)
		{
		}

		public SoundPointCollection(IEnumerable<SoundPoint> collection) : base(collection)
		{
		}

		protected override void ClearItems()
		{
			foreach (var p in this)
				ExcludePoint(p);
			base.ClearItems();
		}

		protected override void InsertItem(int index, SoundPoint item)
		{
			IncludePoint(item);
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, SoundPoint item)
		{
			IncludePoint(item);
			base.SetItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			ExcludePoint(this[index]);
			base.RemoveItem(index);
		}

		private void IncludePoint(SoundPoint APoint)
		{
			APoint.PropertyChanged += Point_PropertyChanged;
			//APoint.RemoveSelf += Point_RemoveSelf;
		}

		//private void Point_RemoveSelf(SoundPoint obj)
		//{
		//	Task.Factory.StartNew(() =>
		//	{
		//		App.Current.Dispatcher.Invoke(new Action(() => Remove(obj)));
		//	});
		//}

		private void ExcludePoint(SoundPoint APoint)
		{
			APoint.PropertyChanged -= Point_PropertyChanged;
			//APoint.RemoveSelf -= Point_RemoveSelf;
		}

		private void Point_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (FChangedLock > 0)
				FChangedNeed = true;
			else
				RaiseChanged(sender, e);
		}

		public void ChangedLock()
		{
			if (FChangedLock == 0)
				FChangedNeed = false;
			FChangedLock++;
		}

		public void ChangedUnlock(bool ARaiseChangedIfNeed = true)
		{
			FChangedLock--;
			if (FChangedLock == 0)
				if (ARaiseChangedIfNeed && FChangedNeed)
					RaiseChanged(null, null);
		}

		private void RaiseChanged(object sender, PropertyChangedEventArgs e)
		{
			PointPropertyChanged?.Invoke(sender, e);
		}
	}
}
