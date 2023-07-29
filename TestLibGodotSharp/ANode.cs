using Godot;

namespace GodotGame;

public partial class ANode : Node3D
{
    /// <inheritdoc />
    public override void _Ready()
    {
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

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventKey key)
        {
            if(key.Keycode == Key.Space)
            {
                if (key.IsPressed())
                {
                    Console.WriteLine("SPACE");
                }
            }
        }
    }
}
