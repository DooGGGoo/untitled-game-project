using Godot;
using System;


[GlobalClass]
public partial class InteractableTest : Node3D, IInteractable
{
    public void Interact(CharacterBody3D interactor)
    {
        GD.Print("Interacted by ", interactor.Name);
        // Change color of child meshinstance3d to random
        MeshInstance3D childMesh = GetNode<MeshInstance3D>("MeshInstance3D");
        OrmMaterial3D mat = new()
        {
            AlbedoColor = Color.Color8((byte)GD.RandRange(0, 255), (byte)GD.RandRange(0, 255), (byte)GD.RandRange(0, 255))
        };
        childMesh.MaterialOverride = mat; 
    }
}