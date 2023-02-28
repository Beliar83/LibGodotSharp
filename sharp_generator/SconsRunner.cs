using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SharpGenerator.Program;

namespace SharpGenerator
{
	public static class SconsRunner
	{

		public static bool RunScons()
		{
			var argsPrepend = "";
			var shellName = "/bin/bash";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				shellName = "cmd";
				argsPrepend = "/c ";
			}
			var psi = new ProcessStartInfo
			{
				FileName = shellName,
				Arguments = argsPrepend + "scons library_type=shared_library debug_symbols=true",
				WorkingDirectory = GodotRootDir
			};
			using var process = Process.Start(psi);
			Console.WriteLine("Scons Running");
			process.WaitForExit();
            Console.WriteLine("Scons Done");
            return process.ExitCode == 0;
		}
	}
}
