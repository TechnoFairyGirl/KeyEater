using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace KeyEaterService
{
	static class Extensions
	{
		public static TResult Let<T, TResult>(this T arg, Func<T, TResult> func) => func(arg);

		public static T Also<T>(this T arg, Action<T> func) { func(arg); return arg; }

		public static int ToInt(this string str) =>
			(int)(new Int32Converter()).ConvertFromString(str);

		public static string Join(this IEnumerable<string> strings, string separator) =>
			string.Join(separator, strings);
	}
}