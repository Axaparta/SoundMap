using Cloo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundMap.Settings
{
	[Serializable]
	public class OpenCLSettings: ICloneable
	{
		private static string[] FPlatformDeviceNames = null;
		private string FPlatformDeviceName = null;

		public OpenCLSettings()
		{ 
		}

		public string PlatformDeviceName
		{
			get
			{
				if (FPlatformDeviceName == null)
					FPlatformDeviceName = PlatformDeviceNames.FirstOrDefault();
				return FPlatformDeviceName;
			}
			set => FPlatformDeviceName = value;
		}

		public static string[] PlatformDeviceNames
		{
			get
			{
				if (FPlatformDeviceNames == null)
				{
					var pdn = new List<string>();
					foreach (var p in ComputePlatform.Platforms)
						foreach (var d in p.Devices)
							pdn.Add(p.Name + " " + d.Name);
					FPlatformDeviceNames = pdn.ToArray();
				}
				return FPlatformDeviceNames;
			}
		}

		public ComputeDevice GetComputeDevice()
		{
			foreach (var p in ComputePlatform.Platforms)
				foreach (var d in p.Devices)
				{
					var pdn = p.Name + " " + d.Name;
					if (pdn == PlatformDeviceName)
						return d;
				}
			return null;
		}

		public OpenCLSettings Clone()
		{
			return new OpenCLSettings()
			{
				FPlatformDeviceName = this.FPlatformDeviceName
			};
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
