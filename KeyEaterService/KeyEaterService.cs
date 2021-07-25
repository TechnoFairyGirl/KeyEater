using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace KeyEaterService
{
	[RunInstaller(true)]
	public class KeyEaterServiceInstaller : Installer
	{
		public KeyEaterServiceInstaller()
		{
			ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
			processInstaller.Account = ServiceAccount.LocalSystem;
			Installers.Add(processInstaller);

			ServiceInstaller serviceInstaller = new ServiceInstaller();
			serviceInstaller.StartType = ServiceStartMode.Automatic;
			serviceInstaller.ServiceName = "KeyEaterService";
			serviceInstaller.Description = "Runs commands when keys are pressed. Works everywhere, even the secure desktop.";
			serviceInstaller.DisplayName = "Key Eater Service";
			Installers.Add(serviceInstaller);
			
		}
	}

	public class KeyEaterService : ServiceBase
	{
		List<Process> processes = new List<Process>();

		public KeyEaterService()
		{
			this.ServiceName = "KeyEaterService";
		}

		public void StartInteractive(string[] args)
		{
			Console.CancelKeyPress += (sender, e) => OnStop();
			OnStart(args);
			Thread.Sleep(Timeout.Infinite);
		}

		Process SpawnOnDekstop(string desktop)
		{
			var serviceExePath = System.Reflection.Assembly.GetEntryAssembly().Location;
			var targetExePath = Path.Combine(Path.GetDirectoryName(serviceExePath), "KeyEater.exe");
			var process = WindowsInterop.CreateProcessInSession(targetExePath, null, desktop, false, 1);

			processes.Add(process);

			return process;
		}

		protected override void OnStart(string[] args)
		{
			SpawnOnDekstop("Default");
			SpawnOnDekstop("Winlogon");
		}

		protected override void OnStop()
		{
			foreach (var process in processes)
				process.Kill();
		}
	}
}
