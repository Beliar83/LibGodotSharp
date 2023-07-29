using Godot;

namespace Nodes; //Needs namespace for code generator

/// <summary>
/// Demo node to show how to register a node to godot
/// </summary>
public partial class DemoSpinningCubeNode : Node3D // Needs to be a partial class for the code generator to extended it
{
    public override void _Ready()
    {
        //Adds a cube mesh for a test visual
        var meshRender = new MeshInstance3D
        {
            Mesh = new BoxMesh()
        };
        AddChild(meshRender);
    }

    public override void _Process(double delta)
    {
        RotateY((float)delta); // rotate the cube to show that the process can be used
    }
}
