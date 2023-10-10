using Microsoft.CodeAnalysis;

namespace Generators;

public static class GenVirtual
{
    public static string TypeToVariantType(ITypeSymbol type)
    {
        return TypeToVariantType(type, LibGodotGenerators.GetSpecialBase(type));
    }
    public static string TypeToVariantType(ITypeSymbol type, SpecialBase sBase)
    {
        return sBase switch
        {
            SpecialBase.Node => "Object",
            SpecialBase.Resource => "Object",
            _ => type.Name switch
            {
                "Boolean" => "Bool",
                "Int64" => "Int",
                "Double" => "Float",
                "String" => "String",
                "Vector2" => "Vector2",
                "Vector2i" => "Vector2i",
                "Rect2" => "Rect2",
                "Rect2i" => "Rect2i",
                "Vector3" => "Vector3",
                "Vector3i" => "Vector3i",
                "Transform2D" => "Transform2D",
                "Vector4" => "Vector4",
                "Vector4i" => "Vector4i",
                "Plane" => "Plane",
                "Quaternion" => "Quaternion",
                "AABB" => "AABB",
                "Basis" => "Basis",
                "Transform3D" => "Transform3D",
                "Projection" => "Projection",
                "Color" => "Color",
                "StringName" => "StringName",
                "NodePath" => "NodePath",
                "RID" => "RID",
                "Callable" => "Callable",
                "Signal" => "Signal",
                "Dictionary" => "Dictionary",
                "Array" => "Array",
                "PackedByteArray" => "PackedByteArray",
                "PackedInt32Array" => "PackedInt32Array",
                "PackedInt64Array" => "PackedInt64Array",
                "PackedFloat32Array" => "PackedFloat32Array",
                "PackedFloat64Array" => "PackedFloat64Array",
                "PackedStringArray" => "PackedStringArray",
                "PackedVector2Array" => "PackedVector2Array",
                "PackedVector3Array" => "PackedVector3Array",
                "PackedColorArray" => "PackedColorArray",
                _ => throw new Exception($"Unknown type: {type.Name}"),
            },
        };
    }

    public static bool Generate(GeneratorExecutionContext context, INamedTypeSymbol c)
    {
        var methods = c.GetMembers().Where(x => x is IMethodSymbol).Select(x => (IMethodSymbol)x);
        var code = $$"""
                     using System.Runtime.CompilerServices;
                     using System.Runtime.InteropServices;
                     using GodotSharpGDExtension;
                     namespace {{c.ContainingNamespace}} {
                     public unsafe partial class {{c.Name}} : {{c.BaseType.Name}} {
                     """;
        var addedCode = "";
        var has = false;
        code += $$"""
                    [UnmanagedCallersOnly]
                      public static unsafe new GDExtensionInterface.GDExtensionClassCallVirtual __RegisterVirtual(void* user_ptr, GDExtensionInterface.GDExtensionConstStringNamePtr name) {
                          var cSharpString = new StringName(name);
                          switch (cSharpString) {
                  """;
        foreach (var method in methods)
        {
            if (method.IsOverride)
            {
                has = true;
                addedCode += $$"""
                               
                                   [UnmanagedCallersOnly]        
                                   private static unsafe void {{method.Name}}_Caller(GDExtensionInterface.GDExtensionClassInstancePtr p_instance, GDExtensionInterface.GDExtensionConstTypePtr* p_args, GDExtensionInterface.GDExtensionTypePtr r_ret) {
                                           var instance = ({{c.Name}})p_instance.Data;

                               """;
                var args = "";
                for (var j = 0; j < method.Parameters.Length; j++)
                {
                    var arg = method.Parameters[j];
                    if (arg.Type.Name == "String")
                    {
                        args += $"StringMarshall.ToManaged(p_args[{j}])";
                    }
                    else if (TypeToVariantType(arg.Type) == "Object")
                    {
                        args += $"({arg.Type})GodotSharpGDExtension.Object.ConstructUnknown(*(void**)p_args[{j}])";
                    }
                    else
                    {
                        args += $"*({arg.Type}*)p_args[{j}]";
                    }
                    if (j < method.Parameters.Length - 1)
                    {
                        args += ", ";
                    }
                }
                if (method.ReturnsVoid) {
                    addedCode += $"\t\t\tinstance.{method.Name}({args});\n";
                }
                else
                {
                    addedCode += $"\t\tvar returnData = instance.{method.Name}({args})\n";
                    if (TypeToVariantType(method.ReturnType) == "Object")
                    {
                        code += $"*(void**)r_ret = returnData._internal_pointer;";
                    }
                    else
                    {
                        code += $"*({method.ReturnType}*)r_ret = returnData;";
                    }
                }
                addedCode += $$"""
                                       }
                               """;
                code += $$"""
                                  
                                      case "{{Renamer.ToSnake(method.Name)}}":
                                          return new GDExtensionInterface.FnPtr_GDExtensionClassInstancePtr_GDExtensionConstTypePtrPtr_GDExtensionTypePtr_Void { Pointer = &{{method.Name}}_Caller };
                          """;
            }
        }
        if (has == false)
        {
            return false;
        }
        code += $$"""
                              
                              default: return new GDExtensionInterface.FnPtr_GDExtensionClassInstancePtr_GDExtensionConstTypePtrPtr_GDExtensionTypePtr_Void();
                          }
                      }
                  """;
        code += addedCode;
        code += $$"""
                  
                      }
                  }
                  """;

        context.AddSource($"{c.Name}.virtual.gen.cs", code);
        return true;
    }
}