using Common;
using Interpolators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SoundMap.Waveforms
{
	[Serializable]
	public class CustomWaveform : BufferWaveform
	{
		private string FCustomName = string.Empty;
		private OneHerzList FOneHerz = null;
		public override string Name => FCustomName;

		public OneHerzList OneHerz
		{
			get
			{
				if (FOneHerz == null)
					FOneHerz = new OneHerzList();
				return FOneHerz;
			}
			set
			{
				if (value == null)
					FOneHerz = new OneHerzList();
				else
					FOneHerz = value.Clone();
				NeedInit = true;
			}
		}

		public string CustomName
		{
			get => FCustomName;
			set
			{
				if (FCustomName != value)
				{
					FCustomName = value;
					NotifyPropertyChanged(nameof(CustomName));
					NotifyPropertyChanged(nameof(Name));
				}
			}
		}

		public CustomWaveform()
		{ }

		public CustomWaveform(string AName, OneHerzList AOneHerz)
		{
			if (AOneHerz != null)
				OneHerz = AOneHerz.Clone();
			CustomName = AName;
			NeedInit = true;
		}

		protected override double[] CreateSample(int ASampleRate)
		{
			return OneHerz.Resample(0, 1, -1, +1, ASampleRate).Select(p => p.Y).ToArray();
		}

		//public override Waveform Clone()
		//{
		//	var r = (CustomWaveform)base.Clone();
		//	r.CustomName = this.FCustomName;
		//	r.OneHerz = this.OneHerz.Clone();
		//	return r;
		//}
	}
}
