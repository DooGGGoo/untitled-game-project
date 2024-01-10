using Godot;
using Godot.Collections;


public partial class WeaponInventory : Node3D
{
    [Export] public Dictionary<WeaponData.WeaponType, Weapon> WeaponSlots = new()
    {
        { WeaponData.WeaponType.Primary, null },
        { WeaponData.WeaponType.Secondary, null },
        { WeaponData.WeaponType.Special, null }
    };

    public void AddWeapon(PackedScene weaponScene)
    {
        if (weaponScene == null)
        {
            GD.Print("WeaponScene is null");
            return;
        }

        Weapon weapon = weaponScene.Instantiate<Weapon>();
        WeaponData.WeaponType weaponType = weapon.WeaponData._WeaponType;

        if (WeaponSlots.TryGetValue(weaponType, out Weapon existingWeapon))
        {
            if (IsInstanceValid(existingWeapon))
            {
                existingWeapon.QueueFree();
            }
        }

        WeaponSlots[weaponType] = weapon;
        AddChild(weapon);

        GD.Print($"Added weapon of type {weaponType} to the slot {WeaponSlots[weaponType]}");

        // Equip animation
        Position = new Vector3(0.095f, -0.11f - 0.15f, -0.12f - -0.15f);
    }
}