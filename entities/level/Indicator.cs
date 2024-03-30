using Godot;

[GlobalClass]
public partial class Indicator : Node3D
{
    public void ChangeColor()
    {
        MeshInstance3D childMesh = GetNode<MeshInstance3D>("MeshInstance3D");
        OrmMaterial3D mat = new()
        {
            AlbedoColor = Color.Color8((byte)GD.RandRange(0, 255), (byte)GD.RandRange(0, 255), (byte)GD.RandRange(0, 255))
        };
        childMesh.MaterialOverride = mat; 
    }
}