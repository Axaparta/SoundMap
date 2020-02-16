using Interpolators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SoundMap
{
	/// <summary>
	/// Точки в диапазоне от 0..1 по х и от 0 (верх) до 1 (низ)
	/// </summary>
	public class OneHerzList : List<Point>, ICloneable, IXmlSerializable
	{
		#region ValueProvider
		private class ValueProvider : IInterpolatorValueProvider
		{
			public double[] XValues { get; }
			public double[] YValues { get; }

			public ValueProvider(IList<Point> AData)
			{
				XValues = new double[AData.Count];
				YValues = new double[XValues.Length];

				for (int i = 0; i < AData.Count; i++)
				{
					var p = AData[i];
					XValues[i] = p.X;
					YValues[i] = p.Y;
				}
			}
		}
		#endregion

		private const string ItemCountName = "ItemCount";
		private const string ItemName = "Item";

		public OneHerzList()
		{
			AddRange(ResetFilter());
		}

		public OneHerzList(IList<Point> list):
			base(list)
		{
		}

		public OneHerzList(IEnumerable<Point> collection):
			base(collection)
		{
		}

		public Point[] MedianaFilter(int halfWindowSize = 3)
		{
			Point[] p = new Point[Count];
			double[] buf = new double[halfWindowSize + halfWindowSize + 1];

			for (int i = 0; i < Count; i++)
			{
				for (int j = i - halfWindowSize, bj = 0; j <= i + halfWindowSize; j++, bj++)
				{
					if (j < 0)
						buf[bj] = this[Count + j].Y;
					else if (j >= Count)
						buf[bj] = this[j - Count].Y;
					else
						buf[bj] = this[j].Y;
				}

				double m = 0;
				for (int j = 0; j < buf.Length; j++)
					m += buf[j];
				m /= buf.Length;
				p[i] = new Point(this[i].X, m);
			}

			return p;
		}

		public Point[] GetExtra(int extraCount)
		{
			if (Count == 0)
				return new Point[] { };

			Point[] r = new Point[Count + extraCount * 2];

			int maxCount = Count * extraCount;

			for (int i = -extraCount; i < Count + extraCount; i++)
			{
				Point p;
				if (i < 0)
				{
					p = this[(maxCount + i) % Count];
					p.X -= (-i - 1) / Count + 1;
				}
				else if (i >= Count)
				{
					p = this[(maxCount + i) % Count];
					p.X += (i - Count) / Count + 1;
				}
				else
					p = this[i];
				r[i + extraCount] = p;
			}

			return r;
		}

		public Point[] Resample(double minX, double maxX, double minY, double maxY, int count)
		{
			if (Count == 0)
				return new Point[] { };

			// В краевых положениях имитируется наличие +2х точек
			ValueProvider vp = new ValueProvider(GetExtra(3));
			Interpolator inter = new AkimaSplineInterpolator();

			inter.CreateModel(vp);

			Point[] r = new Point[count];

			// Заполнить r точками от 0..1
			if (count == 1)
			{
				double centerX = 0.5;
				r[0] = new Point(centerX, inter.Evaluate(centerX));
			}
			else
			{
				double stepX = 1D / (count - 1);
				double x = 0;
				for (int i = 0; i < count; i++, x += stepX)
					r[i] = new Point(x, inter.Evaluate(x));
			}

			// Масштабирование до max-min'ов.
			double dY = maxY - minY;
			double dX = maxX - minX;
			for (int i = 0; i < count; i++)
			{
				var p = r[i];
				r[i] = new Point(minX + p.X * dX, minY + p.Y * dY);
			}

			return r;
		}

		public Point[] SineGenerate(double AWidth)
		{
			Point[] r = new Point[(int)AWidth];

			for (int i = 0; i < r.Length; i++)
			{
				var x = (double)i / r.Length;
				r[i] = new Point(x, 0.5 + 0.5*Math.Sin(2 * Math.PI * x));
			}

			return r;
		}

		public Point[] HalfOffsetFilter()
		{
			List<Point> neg = new List<Point>(Count);
			List<Point> other = new List<Point>(Count);

			for (int i = 0; i < Count; i++)
			{
				var p = this[i];
				var px = p.X - 0.5;
				if (px < 0)
					neg.Add(new Point(px + 1, p.Y));
				else
					other.Add(new Point(px, p.Y));
			}

			other.AddRange(neg);

			return other.ToArray();
		}

		public Point[] NormalizeFilter()
		{
			double maxY;
			double minY;
			maxY = minY = this[0].Y;
			for (int i = 1; i < Count; i++)
			{
				var y = this[i].Y;
				if (y > maxY)
				{
					maxY = y;
					continue;
				}
				if (y < minY)
					minY = y;
			}

			Point[] r;

			if (maxY != minY)
			{
				r = new Point[Count];
				double k = maxY - minY;
				for (int i = 0; i < Count; i++)
					r[i] = new Point(this[i].X, (this[i].Y - minY) / k);
			}
			else
				r = ToArray();

			return r;
		}

		public Point[] ResetFilter()
		{
			return new Point[]
			{
				new Point(0, 0.5)
			};
		}

		public OneHerzList Clone()
		{
			var r = new OneHerzList();
			r.Clear();
			r.AddRange(this);
			return r;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			try
			{
				using (var subReader = reader.ReadSubtree())
				{
					var r = subReader;
					r.MoveToContent();
					if (!int.TryParse(r.GetAttribute(ItemCountName), out var sc))
						throw new Exception($"{ItemCountName} not defined");

					Clear();
					for (int i = 0; i < sc; i++)
					{
						// StartElement: Item
						r.Read();

						// Text
						r.Read();
						//Debug.WriteLine($"{r.NodeType}; {r.Name}; {r.Value}");
						Add(DataStringToPoint(r.Value));

						// EndElement
						r.Read(); 
					}
				}

				reader.Read();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(ItemCountName, Count.ToString());

			foreach (var s in this)
			{
				writer.WriteStartElement(ItemName);
				writer.WriteString(PointToDataString(s));
				writer.WriteEndElement();
			}
		}

		public static string PointToDataString(Point p)
		{
			List<byte> d = new List<byte>(2 * sizeof(double));
			d.AddRange(BitConverter.GetBytes(p.X));
			d.AddRange(BitConverter.GetBytes(p.Y));
			return Convert.ToBase64String(d.ToArray());
		}

		public static Point DataStringToPoint(string data)
		{
			var d = Convert.FromBase64String(data);
			Point p = new Point();
			p.X = BitConverter.ToDouble(d, 0);
			p.Y = BitConverter.ToDouble(d, sizeof(double));
			return p;
		}
	}
}
