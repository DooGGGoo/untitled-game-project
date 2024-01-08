using Godot;
using System;

[GlobalClass]
public partial class WeaponSpawner : StaticBody3D, IInteractable
{
    [Export] public PackedScene WeaponScene;

    public override void _Ready()
    {
        if (WeaponScene == null)
        {
            GD.PrintS(Name, "Weapon scene is null");
        }
    }

    public void Interact(CharacterBody3D interactor)
    {
        if (interactor as Player != null)
        {
            Player player = interactor as Player;

            player.AddWeapon(WeaponScene);
        }
    }
}