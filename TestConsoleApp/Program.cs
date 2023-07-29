using GodotGame;
using LibGodotSharp;

namespace TestConsoleApp
{
    internal class Program
    {
        static unsafe int Main(string[] args)
        {
            return LibGodotManager.RunGodot(Array.Empty<string>(), MainGodotGame.LoadScene,  MainGodotGame.LoadProjectSettings, new []{ typeof(MainGodotGame).Assembly},true);
        }
    }
}