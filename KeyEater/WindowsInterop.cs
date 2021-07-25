using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyEater
{
	static class WindowsInterop
	{
		const int HC_ACTION = 0;
		const int WH_KEYBOARD_LL = 13;
		const uint LLKHF_EXTENDED = 0x01;

		static Dictionary<IntPtr, LowLevelKeyboardProc> keyboardCallbacks = new Dictionary<IntPtr, LowLevelKeyboardProc>();

		[StructLayout(LayoutKind.Sequential)]
		struct KBDLLHOOKSTRUCT
		{
			public uint vkCode;
			public uint scanCode;
			public uint flags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern ushort GetAsyncKeyState(int vKey);

		public static IntPtr InstallKeyHook(Func<Keys, uint, bool, bool> keyFunc)
		{
			var callback = new LowLevelKeyboardProc((nCode, wParam, lParam) =>
			{
				if (nCode == HC_ACTION)
				{
					var keyDown = ((uint)wParam & 0x0001) == 0;
					var keyData = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
					var keyCode = (Keys)keyData.vkCode;
					var extendedKey = (keyData.flags & LLKHF_EXTENDED) != 0;
					var scanCode = keyData.scanCode | (extendedKey ? 0xE000u : 0);

					if (keyFunc(keyCode, scanCode, keyDown))
						return (IntPtr)1;
				}

				return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			});

			var keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, callback, IntPtr.Zero, 0);
			if (keyHook == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			lock (keyboardCallbacks)
				keyboardCallbacks.Add(keyHook, callback);

			return keyHook;
		}

		public static void UninstallKeyHook(IntPtr keyHook)
		{
			if (!UnhookWindowsHookEx(keyHook))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			lock (keyboardCallbacks)
				keyboardCallbacks.Remove(keyHook);
		}

		public static bool GetKeyState(Keys key)
		{
			return (GetAsyncKeyState((int)key) & 0x8000) != 0;
		}
	}
}
