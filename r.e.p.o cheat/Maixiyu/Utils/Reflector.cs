namespace r.e.p.o_cheat;

public static class Reflector
{
	public static ReflectionHelper<T> Reflect<T>(this T obj)
	{
		return new ReflectionHelper<T>(obj);
	}
}
