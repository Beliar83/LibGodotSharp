using GDExtension;
using Godot;

namespace GodotGame;

public partial class HelloFromLibGodot : Node3D
{
    public override void _Ready()
    {
        SetProcess(true);
        var meshRender = new MeshInstance3D
        {
            Mesh = new BoxMesh()
        };
        AddChild(meshRender);
    }

    public override void _Process(double delta)
    {
        RotateY((float)delta);
    }
}
