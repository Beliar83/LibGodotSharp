using Microsoft.CodeAnalysis;

namespace Generators;

public static class Register
{

	public enum Level
	{
		Scene,
		Editor,
	}

	public struct Data
	{
		public string name;
		public string godotName;
		public string @base;
		public string @namespace;
		public Level level;
	}

	public static Data Generate(GeneratorExecutionContext context, INamedTypeSymbol c, bool notification, bool virtualgen, SpecialBase sBase)
	{
		var gdName = (string)(c.GetAttributes().
					SingleOrDefault(x => x.AttributeClass.Name == "RegisterAttribute").NamedArguments.
					SingleOrDefault(x => x.Key == "name").Value.Value ?? c.Name
			);
		var isRefCounted = sBase switch
		{
			SpecialBase.Resource => true,
			SpecialBase.RefCounted => true,
			_ => false,
		};

		string registerBase = c.BaseType is not null ? $"\n\t\t{c.BaseType.ContainingNamespace.Name}.{c.BaseType.Name}.Register();" : "";
		
		var source = $$"""
		               using System.Runtime.CompilerServices;
		               using System.Runtime.InteropServices;
		               using GodotSharpGDExtension;
		               using System;
		               using static GDExtension.Native;
		               namespace {{c.ContainingNamespace}} {
		               public unsafe partial class {{c.Name}} : {{c.BaseType.Name}} {
		               #pragma warning disable CS8618
		               	public static new StringName __godot_name;
		               #pragma warning restore CS8618
		               	private GCHandle handle;
		               #pragma warning disable CS8618
		               	public {{c.Name}}() {
		               		handle = GCHandle.Alloc(this {{(isRefCounted ? ", GCHandleType.Weak" : "")}});
		               		GDExtensionMain.extensionInterface.object_set_instance.Data.Pointer(_internal_pointer, __godot_name._internal_pointer, (void*)this);
		               	}
		               #pragma warning restore CS8618
		               	public static explicit operator void*({{c.Name}} instance) => (void*)(IntPtr)instance.handle;
		               	public static explicit operator {{c.Name}}(void* ptr) => ({{c.Name}})(GCHandle.FromIntPtr(new IntPtr(ptr)).Target!);
		               	public static {{c.Name}} Construct(void* ptr) {
		               		ptr = (void*)*(((IntPtr*)ptr) + 2);
		               		return ({{c.Name}})ptr;
		               	}
		               	public static unsafe new void Register() {	
		               		if (!GodotSharpGDExtension.Object.RegisterConstructor("{{c.Name}}", Construct)) return;{{ registerBase }}
		               		__godot_name = new StringName("{{gdName}}");
		               		var info = new GDExtensionInterface.GDExtensionClassCreationInfo() {
		               			is_virtual = System.Convert.ToByte(false),
		               			is_abstract = System.Convert.ToByte({{c.IsAbstract.ToString().ToLower()}}),
		               			//set_func = &SetFunc,
		               			//get_func = &GetFunc,
		               			//get_property_list_func = &GetPropertyList,
		               			//free_property_list_func = &FreePropertyList,
		               			//property_can_revert_func = &PropertyCanConvert,
		               			//property_get_revert_func = &PropertyGetRevert,
		               			notification_func = new GDExtensionInterface.FnPtr_GDExtensionClassInstancePtr_Int_Void { Pointer = {{(notification ? "Engine.IsEditorHint() ? null : &__Notification" : "null")}}},
		               			//to_string_func = &ToString,
		               			//reference_func = &Reference,
		               			//unreference_func = &Unreference,
		               			create_instance_func = new GDExtensionInterface.FnPtr_VoidPtr_GDExtensionObjectPtr { Pointer = &CreateObject },
		               			free_instance_func = new GDExtensionInterface.FnPtr_VoidPtr_GDExtensionClassInstancePtr_Void { Pointer = &FreeObject },
		               			get_virtual_func = new GDExtensionInterface.FnPtr_VoidPtr_GDExtensionConstStringNamePtr_GDExtensionClassCallVirtual { Pointer = {{(virtualgen ? "&__RegisterVirtual" : "null")}} },
		               			//get_rid_func = &GetRid,
		               		};
		               		GDExtensionMain.extensionInterface.classdb_register_extension_class.Data.Pointer(GDExtensionMain.library, __godot_name._internal_pointer, {{c.BaseType.Name}}.__godot_name._internal_pointer, &info);
		               		RegisterMethods();
		               		RegisterExports();
		               		RegisterSignals();
		               	}
		               	
		               	[UnmanagedCallersOnly]
		               	static unsafe GDExtensionInterface.GDExtensionObjectPtr CreateObject(void* userdata) {
		               		var instance = new {{c.Name}}();
		               		return (void*)instance._internal_pointer;
		               	}
		               	
		               	[UnmanagedCallersOnly]
		               	static unsafe void FreeObject(void* userdata, GDExtensionInterface.GDExtensionClassInstancePtr instancePtr) {
		               		var instance = ({{c.Name}})instancePtr.Data;
		               		instance.handle.Free();
		               	}
		               }
		               }
		               """;

		context.AddSource($"{c.Name}.reg.gen.cs", source);

		var editorOnly = (bool)(c.GetAttributes().
					SingleOrDefault(x => x.AttributeClass.Name == "RegisterAttribute").NamedArguments.
					SingleOrDefault(x => x.Key == "editorOnly").Value.Value ?? false
			);
		var level = editorOnly switch
		{
			true => Level.Editor,
			false => Level.Scene,
		};
		return new Data()
		{
			name = c.Name,
			godotName = gdName,
			@base = c.BaseType.Name,
			@namespace = c.ContainingNamespace.ToString(),
			level = level,
		};
	}
}