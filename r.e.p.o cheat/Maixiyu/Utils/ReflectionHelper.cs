using System;
using System.Reflection;

namespace r.e.p.o_cheat;

public class ReflectionHelper<T>
{
	private readonly T instance;

	private readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private readonly Type type;

	public ReflectionHelper(T obj)
	{
		instance = obj;
		type = typeof(T);
	}

	public ReflectionHelper<T> SetValue(string fieldName, object value)
	{
		FieldInfo field = type.GetField(fieldName, flags);
		if (field != null)
		{
			field.SetValue(instance, value);
		}
		return this;
	}

	public object GetValue(string fieldName)
	{
		FieldInfo field = type.GetField(fieldName, flags);
		if (!(field != null))
		{
			return null;
		}
		return field.GetValue(instance);
	}
}
