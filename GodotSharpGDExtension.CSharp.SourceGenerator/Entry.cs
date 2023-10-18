using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Generators;

public static class Entry
{

    public static void Execute(GeneratorExecutionContext context, List<Register.Data> classes)
    {
        var registrations = "";
        var editorRegistrations = "";
        var unregistrations = "";

        var classNames = new List<string>();


        foreach (Register.Data n in classes)
        {
            switch (n.level)
            {
                case Register.Level.Scene:
                    registrations += $"{n.@namespace}.{n.name}.Register();\n\t\t\t";
                    break;
                case Register.Level.Editor:
                    editorRegistrations += $"{n.@namespace}.{n.name}.Register();\n\t\t\t";
                    break;
            }
            unregistrations = $"GDExtensionMain.extensionInterface.classdb_unregister_extension_class.Data.Pointer(GDExtensionMain.library, {n.@namespace}.{n.name}.__godot_name._internal_pointer);\n\t\t\t" + unregistrations;
        }
        Debug.WriteLine($"Building Entry {registrations}");
        var assemblyName = context.Compilation.AssemblyName ?? "NoName";
        var className = $"{assemblyName}ExtensionEntry";
        classNames.Add(className);
        var source = $$"""
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GodotSharpGDExtension;
using static GDExtension.Native;
namespace {{assemblyName}};

public unsafe static class {{className}} {
    [UnmanagedCallersOnly]
    public static void Bind(GDExtensionInterface.GDExtensionClassLibraryPtr libraryPtr,
        GDExtensionInterface.interface_functions interfaceFunctions, delegate* unmanaged<GDExtensionInterface.GDExtensionObjectPtr, GDExtensionInterface.GDExtensionConstTypePtr, void> t2)
    {
		GDExtensionMain.Initialize(libraryPtr, interfaceFunctions, t2);
        //var extensionData = new GDExtensionInterface.extension_data();
        //extensionData.initialize.Pointer = &Initialize;
        //extensionData.uninitialize.Pointer = &Uninitialize;
        //extensionData.minimumInitializationLevel =
        //    GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE;
        //return &extensionData;
    }

    //[UnmanagedCallersOnly]
    //public static void Initialize(GDExtensionInterface.GDExtensionInitializationLevel level) 
    //{
    //    switch (level)
    //    {
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE:
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_SERVERS:
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_SCENE:
    //            // Register all not yet implicitly registered godot classes
    //            Register.RegisterBuiltin();
    //            Register.RegisterUtility();
    //            Register.RegisterCore();
    //            Register.RegisterServers();
    //            Register.RegisterScene();
    //            {{registrations}}
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_EDITOR:
    //            // Register all not yet implicitly registered godot editor classes
    //            Register.RegisterEditor();
    //            {{editorRegistrations}}
    //            break;
    //    }
    //}
    //
    //[UnmanagedCallersOnly]
    //public static void Uninitialize(GDExtensionInterface.GDExtensionInitializationLevel level)
    //{
    //    switch (level)
    //    {
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_CORE:
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_SERVERS:
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_SCENE:
    //            {{unregistrations}}
    //            break;
    //        case GDExtensionInterface.GDExtensionInitializationLevel.GDEXTENSION_INITIALIZATION_EDITOR:
    //            break;
    //    }
    //}
}
""";
        context.AddSource("ExtensionEntry.gen.cs", source);
    }
}