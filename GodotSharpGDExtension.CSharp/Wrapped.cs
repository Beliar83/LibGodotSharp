namespace GodotSharpGDExtension;

public unsafe class Wrapped
{

    public void* _internal_pointer;

    protected Wrapped(StringName type)
    {
        Console.WriteLine(type.Capitalize());
        _internal_pointer = GDExtensionMain.extensionInterface.classdb_construct_object.Data.Pointer(type._internal_pointer);
    }
    protected Wrapped(void* data) => _internal_pointer = data;
    public static unsafe void __Notification(void* instance, int what) { }
    public static unsafe void* __RegisterVirtual(void* userData, void* stringData) { return null; }

    public static void Register() { }
}
