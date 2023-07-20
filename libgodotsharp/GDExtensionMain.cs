using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GDExtension
{
	public unsafe static class GDExtensionMain
	{
		public static Native.GDExtensionInterfaceGetProcAddress getProcAddress;
		public static void* library;
	}
}
