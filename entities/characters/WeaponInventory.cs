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

        foreach (WeaponData.WeaponType slotType in WeaponSlots.Keys)
        {
            if (slotType == weaponType)
            {
                if (IsInstanceValid(WeaponSlots[slotType]))
                {
                    Weapon weaponToRemove = WeaponSlots[slotType];
                    weaponToRemove.QueueFree();
                }

                WeaponSlots[slotType] = weapon;

                AddChild(weapon);
                GD.PrintS("Weapon", weapon, "added to slot", slotType);
                break;
            }

            else
            {
                GD.PrintS("Weapon", weapon, "doesn't fit in the slot", slotType, "Weapon in this slot:", WeaponSlots[slotType]);
            }
        }
    }
}