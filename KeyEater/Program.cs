using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Forms;

namespace KeyEater
{
	class Config
	{
		public class KeyMapping
		{
			public Keys[] Keys { get; set; }
			public string[] Command { get; set; }
		}

		public KeyMapping[] KeyMappings { get; set; }
	}

	static class Program
	{
		static Config config;

		static void KeyComboPressed(string[] command)
		{
			Console.WriteLine(command.Join(" "));
			Util.StartProcess(command.First(), command.Skip(1).Join(" "), false);
		}

		static void Main(string[] args)
		{
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

			var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			var configPath = Path.Combine(Path.GetDirectoryName(exePath), "config.json");
			config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

			var keyCodes = new HashSet<Keys>();

			WindowsInterop.InstallKeyHook((keyCode, scanCode, keyDown) =>
			{
				keyCodes.RemoveWhere(kc => !WindowsInterop.GetKeyState(kc));

				bool changed;
				if (keyDown) changed = keyCodes.Add(keyCode);
				else changed = keyCodes.Remove(keyCode);

				bool sendKey = true;
				
				if (changed && keyDown)
				{
					foreach (var keyMapping in config.KeyMappings)
					{
						if (keyCodes.SetEquals(keyMapping.Keys))
						{
							sendKey = false;
							Task.Run(() => KeyComboPressed(keyMapping.Command));
						}
					}
				}

				return !sendKey;
			});

			Application.Run();
		}
	}
}
