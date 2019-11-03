using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SoundMap
{
	public class SoundPointCollection : ObservableCollection<SoundPoint>
	{
		private int FChangedLock = 0;
		private bool FChangedNeed = false;

		public SoundPointCollection()
		{
		}

		public SoundPointCollection(List<SoundPoint> list) : base(list)
		{
		}

		public SoundPointCollection(IEnumerable<SoundPoint> collection) : base(collection)
		{
		}

		public void AddSoundPoint(SoundPoint APoint)
		{
			APoint.PropertyChanged += Point_PropertyChanged;
			Add(APoint);
		}

		public void RemoveSoundPoint(SoundPoint APoint)
		{
			APoint.PropertyChanged -= Point_PropertyChanged;
			Remove(APoint);
		}

		private void Point_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (FChangedLock > 0)
				FChangedNeed = true;
			else
				RaiseChanged();
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
				if ((ARaiseChangedIfNeed) && (FChangedNeed))
					RaiseChanged();
		}

		private void RaiseChanged()
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
