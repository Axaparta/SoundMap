using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Common
{
	public static class XmlHelper
	{
		public static void Save(object AObject, Stream AStream)
		{
			XmlSerializer xml = new XmlSerializer(AObject.GetType());
			XmlTextWriter writer = new XmlTextWriter(AStream, Encoding.UTF8)
			{
				IndentChar = '\t',
				Indentation = 1,
				Formatting = Formatting.Indented
			};
			xml.Serialize(writer, AObject);
		}

		public static void Save(object AObject, string AFileName)
		{
			var dn = Path.GetDirectoryName(AFileName);
			if ((dn.Length > 0) && !Directory.Exists(dn))
				Directory.CreateDirectory(dn);
			using (var s = File.Create(AFileName))
				Save(AObject, s);
		}

		public static T Load<T>(Stream AStream)
		{
			XmlSerializer xml = new XmlSerializer(typeof(T));
			return (T)xml.Deserialize(AStream);
		}

		public static T Load<T>(string AFileName)
		{
			using (var s = File.OpenRead(AFileName))
				return Load<T>(s);
		}

		public static T Load<T>(string AFileName, T ADefault)
		{
			try
			{
				using (var s = File.OpenRead(AFileName))
					return Load<T>(s);
			}
			catch
			{
				return ADefault;
			}
		}

		public static string ToString(object AObject)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Save(AObject, ms);
				ms.Position = 0;
				using (StreamReader sr = new StreamReader(ms, Encoding.UTF8, true))
					return sr.ReadToEnd();
			}
		}
	}
}
