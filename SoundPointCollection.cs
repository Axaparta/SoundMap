using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SoundMap
{
	public class SoundPointCollection : ObservableCollection<SoundPoint>
	{
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
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
