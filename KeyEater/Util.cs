using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace KeyEater
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

	static class Util
	{
		public static Process StartProcess(
			string executablePath,
			string arguments,
			bool visible = true,
			Action<string> outputCb = null,
			Action<int> exitCb = null)
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = executablePath;
			startInfo.Arguments = arguments;
			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = !visible;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;

			var process = new Process();
			process.StartInfo = startInfo;
			process.EnableRaisingEvents = true;

			if (outputCb != null)
			{
				process.OutputDataReceived += (sender, e) => outputCb(e.Data);
				process.ErrorDataReceived += (sender, e) => outputCb(e.Data);
			}

			if (exitCb != null)
				process.Exited += (sender, e) => exitCb(process.ExitCode);

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			return process;
		}
	}
}
