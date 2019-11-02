using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

public class Observable : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

#if NET45
	protected void NotifyPropertyChanged([CallerMemberName] String propertyName = null)
	{
		if (PropertyChanged != null)
		{
			CheckPropertyName(propertyName);
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
#else
	protected virtual void NotifyPropertyChanged(String propertyName)
	{
		if (PropertyChanged != null)
		{
			CheckPropertyName(propertyName);
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
#endif

	protected void NotifyPropertyChanged(Expression<Func<object>> expression)
	{
		var lambda = expression as LambdaExpression;
		MemberExpression memberExpression;
		if (lambda.Body is UnaryExpression)
		{
			var unaryExpression = lambda.Body as UnaryExpression;
			memberExpression = unaryExpression.Operand as MemberExpression;
		}
		else
		{
			memberExpression = lambda.Body as MemberExpression;
		}
		if (memberExpression != null)
		{
			var propertyInfo = memberExpression.Member as PropertyInfo;
			if (propertyInfo != null)
				NotifyPropertyChanged(propertyInfo.Name);
		}
	}

	[Conditional("DEBUG")]
	private void CheckPropertyName(String propertyName)
	{
		Type type = this.GetType();
		Debug.Assert(type.GetProperty(propertyName) != null, string.Format("Свойство {0} не существует в типе {1}", propertyName, type.FullName));
	}
}