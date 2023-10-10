using System.Runtime.CompilerServices;

namespace GodotSharpGDExtension;

public unsafe partial class PackedVector2Array
{

    public Vector2 this[int index]
    {
        get => this[(long)index];
        set => this[(long)index] = value;
    }

    public Vector2 this[long index]
    {
        get
        {
            return Unsafe.Read<Vector2>(GDExtensionMain.extensionInterface.packed_vector2_array_operator_index.Data.Pointer(_internal_pointer, index));
        }
        set
        {
            Set(index, value);
        }
    }

    public static implicit operator PackedVector2Array(List<Vector2> self)
    {
        var data = new PackedVector2Array();
        data.Resize(self.Count);
        for (int i = 0; i < self.Count; i++)
        {
            data[i] = self[i];
        }
        return data;
    }

    public static implicit operator PackedVector2Array(Vector2[] self)
    {
        var data = new PackedVector2Array();
        data.Resize(self.Length);
        for (int i = 0; i < self.Length; i++)
        {
            data[i] = self[i];
        }
        return data;
    }

    public static implicit operator PackedVector2Array(Span<Vector2> self)
    {
        var data = new PackedVector2Array();
        data.Resize(self.Length);
        for (int i = 0; i < self.Length; i++)
        {
            data[i] = self[i];
        }
        return data;
    }
}
