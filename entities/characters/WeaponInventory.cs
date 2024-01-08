using Godot;
using Godot.Collections;


public partial class WeaponInventory : Node3D
{
    public void AddWeapon(PackedScene weaponScene)
    {
        // TODO: Remove this and add proper weapon switching
        if (IsInstanceValid(GetChild(0)))
        {
            GetChild(0).QueueFree();
        }

        if (weaponScene == null)
        {
            GD.Print("WeaponScene is null");
            return;
        }
        
        Weapon weapon = weaponScene.Instantiate<Weapon>();
        AddChild(weapon);
    }
}