using System.Reflection;
using LibGodotSharp;

namespace DesktopPlatform;

internal class Program
{
    static unsafe int Main(string[] args)
    {
        var runVerbose = false;
#if DEBUG
        runVerbose = true;
#endif
        //Passes arguments down to godot
        return LibGodotManager.RunGodot(args, GodotApplication.LoadScene, GodotApplication.LoadProjectSettings, new[] {typeof(GodotApplication).Assembly}, runVerbose);
    }
}
