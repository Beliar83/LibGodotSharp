using System.Diagnostics;
using System.Runtime.InteropServices;
using GodotSharpGDExtension;

namespace DemoLibrary;

[Register]
public partial class Class1 : MeshInstance3D
{
    
    
    /// <inheritdoc />
    public override void _Ready()
    {
        unsafe
        {
            // if (!Debugger.IsAttached) Console.WriteLine("Attach debugger to continue");
            // while (!Debugger.IsAttached)
            // {
            // }
            Console.WriteLine("Getting mesh");
            var tmp = Mesh;
            Console.WriteLine(tmp.ResourcePath);
            // Console.WriteLine("Setting mesh to null");
            // Mesh = null;
            AABB aabb = tmp.GetAabb();
            Console.WriteLine($"{aabb.size.x}.{aabb.size.y}.{aabb.size.z}");
            Console.WriteLine("Calling test1");
            var __args = stackalloc GDExtensionInterface.GDExtensionConstTypePtr[1];
            __args[0] = tmp._internal_pointer;
            GDExtensionMain.test2(_internal_pointer, __args);
            // Console.WriteLine($"Mesh is: {Mesh}");
            // Console.WriteLine("Setting mesh");
            // Mesh = tmp;
            // {
            //     Size = new Vector3(10, 10, 10),
            // };
        }
    }
}
