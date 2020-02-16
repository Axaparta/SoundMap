using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

// Аппроксимация - нахождение кривой, описывающей промежуточные значения по некоторым точкам.
// Интерполяция - такая аппроксимация, кривая которой точно проходит через точки.
namespace Interpolators
{
	/// <summary>Базовый класс для всяких аппроксимаций</summary>
	public abstract class Interpolator
	{
		protected double[] FXValues = null;
		protected double[] FYValues = null;

		/// <summary>Вычисление промежуточного значения</summary>
		/// <param name="X">Величина по оси X</param>
		/// <returns>Вычисленное значение по оси Y</returns>
		public abstract double Evaluate(double X);

		/// <summary>Отображаемое название метода</summary>
		public string Name
		{
			get
			{
				return GetDescription(GetType());
			}
		}

		public static string GetDescription(Type AType)
		{
			var attr = AType.GetCustomAttribute<DescriptionAttribute>();
			if (attr != null)
				return attr.Description;
			return null;
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>Создание модели для текущего набора узлов</summary>
		public void CreateModel(IInterpolatorValueProvider AValueProvider)
		{
			if (AValueProvider == null)
				throw new ArgumentNullException("AValueProvider");

			FXValues = AValueProvider.XValues;
			FYValues = AValueProvider.YValues;
			CheckXYValues();

			InternalCreateModel();
		}

		[Conditional("DEBUG")]
		private void CheckXYValues()
		{
			if ((FXValues == null) || (FXValues.Length == 0))
				throw new Exception("FXValues должен содержать хотя бы одну точку!");
			if ((FYValues == null) || (FYValues.Length == 0))
				throw new Exception("FValues должен содержать хотя бы одну точку!");
			if (FXValues.Length != FYValues.Length)
				throw new Exception("FXValues.Length != FYValues.Length");
		}

		protected abstract void InternalCreateModel();
	}
}
