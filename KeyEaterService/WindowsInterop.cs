using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace KeyEaterService
{
	static class WindowsInterop
	{
		const uint DETACHED_PROCESS = 0x00000008;
		const uint STARTF_USESHOWWINDOW = 0x00000001;
		const ushort SW_HIDE = 0;
		const ushort SW_SHOWNORMAL = 1;
		const uint MAXIMUM_ALLOWED = 0x02000000;
		const int SecurityDelegation = 3;
		const int TokenPrimary = 1;
		const int TokenSessionId = 12;

		[StructLayout(LayoutKind.Sequential)]
		struct STARTUPINFO
		{
			public uint cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public uint dwX;
			public uint dwY;
			public uint dwXSize;
			public uint dwYSize;
			public uint dwXCountChars;
			public uint dwYCountChars;
			public uint dwFillAttribute;
			public uint dwFlags;
			public ushort wShowWindow;
			public ushort cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public uint dwProcessId;
			public uint dwThreadId;
		}

		delegate bool EnumDesktopProc(string lpszDesktop, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr GetProcessWindowStation();

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool EnumDesktops(IntPtr hwinsta, EnumDesktopProc lpEnumFunc, IntPtr lParam);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CreateProcess(
			string lpApplicationName,
			string lpCommandLine,
			IntPtr lpProcessAttributes,
			IntPtr lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			[In] ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool CreateProcessAsUser(
			IntPtr hToken,
			string lpApplicationName,
			string lpCommandLine,
			IntPtr lpProcessAttributes,
			IntPtr lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			[In] ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetCurrentProcess();

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool DuplicateTokenEx(
			IntPtr hExistingToken,
			uint dwDesiredAccess,
			IntPtr lpTokenAttributes,
			int impersonationLevel,
			int tokenType,
			out IntPtr phNewToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool SetTokenInformation(
			IntPtr TokenHandle,
			int TokenInformationClass,
			ref uint TokenInformation,
			uint TokenInformationLength);

		public static string[] GetDesktops()
		{
			var windowStation = GetProcessWindowStation();
			if (windowStation == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			var desktops = new List<string>();
			if (!EnumDesktops(windowStation, (lpszDesktop, lParam) => { desktops.Add(lpszDesktop); return true; }, IntPtr.Zero))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			return desktops.ToArray();
		}

		public static Process CreateProcess(string executablePath, string arguments, string desktop = null, bool visible = true)
		{
			var processToken = IntPtr.Zero;
			var processInfo = new PROCESS_INFORMATION();

			try
			{
				var startInfo = new STARTUPINFO();
				startInfo.cb = (uint)Marshal.SizeOf<STARTUPINFO>();
				startInfo.lpDesktop = desktop;
				startInfo.dwFlags = STARTF_USESHOWWINDOW;
				startInfo.wShowWindow = visible ? SW_SHOWNORMAL : SW_HIDE;

				if (!CreateProcess(
					executablePath,
					arguments,
					IntPtr.Zero,
					IntPtr.Zero,
					false,
					DETACHED_PROCESS,
					IntPtr.Zero,
					null,
					ref startInfo,
					out processInfo))
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			finally
			{
				if (processInfo.hProcess != IntPtr.Zero)
					CloseHandle(processInfo.hProcess);

				if (processInfo.hThread != IntPtr.Zero)
					CloseHandle(processInfo.hThread);
			}

			return Process.GetProcessById((int)processInfo.dwProcessId);
		}

		public static Process CreateProcessInSession(
			string executablePath,
			string arguments,
			string desktop = null,
			bool visible = true,
			uint sessionId = 1)
		{
			var processToken = IntPtr.Zero;
			var newProcessToken = IntPtr.Zero;
			var processInfo = new PROCESS_INFORMATION();

			try
			{
				if (!OpenProcessToken(GetCurrentProcess(), MAXIMUM_ALLOWED, out processToken))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (!DuplicateTokenEx(processToken, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityDelegation, TokenPrimary, out newProcessToken))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (!SetTokenInformation(newProcessToken, TokenSessionId, ref sessionId, sizeof(uint)))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				var startInfo = new STARTUPINFO();
				startInfo.cb = (uint)Marshal.SizeOf<STARTUPINFO>();
				startInfo.lpDesktop = desktop;
				startInfo.dwFlags = STARTF_USESHOWWINDOW;
				startInfo.wShowWindow = visible ? SW_SHOWNORMAL : SW_HIDE;

				if (!CreateProcessAsUser(
					newProcessToken,
					executablePath,
					arguments,
					IntPtr.Zero,
					IntPtr.Zero,
					false,
					DETACHED_PROCESS,
					IntPtr.Zero,
					null,
					ref startInfo,
					out processInfo))
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			finally
			{
				if (processToken != IntPtr.Zero)
					CloseHandle(processToken);

				if (newProcessToken != IntPtr.Zero)
					CloseHandle(newProcessToken);

				if (processInfo.hProcess != IntPtr.Zero)
					CloseHandle(processInfo.hProcess);

				if (processInfo.hThread != IntPtr.Zero)
					CloseHandle(processInfo.hThread);
			}

			return Process.GetProcessById((int)processInfo.dwProcessId);
		}
	}
}
