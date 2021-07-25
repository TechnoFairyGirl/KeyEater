using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace KeyEaterService
{
	static class Program
	{
		static void Main(string[] args)
		{
			var service = new KeyEaterService();
			if (args.Length >= 1 && args[0] == "/interactive") service.StartInteractive(args);
			else ServiceBase.Run(new[] { service });
		}
	}
}
