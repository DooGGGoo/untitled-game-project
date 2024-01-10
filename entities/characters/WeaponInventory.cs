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

    [Export] public WeaponData.WeaponType SelectedSlot = WeaponData.WeaponType.Primary;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("slot_1"))
        {
            SelectWeapon(WeaponData.WeaponType.Primary);
        }
        else if (Input.IsActionJustPressed("slot_2"))
        {
            SelectWeapon(WeaponData.WeaponType.Secondary);
        }
        else if (Input.IsActionJustPressed("slot_3"))
        {
            SelectWeapon(WeaponData.WeaponType.Special);
        }
    }

    public void AddWeapon(PackedScene weaponScene)
    {
        if (weaponScene == null)
        {
            GD.Print("Can't add new weapon: WeaponScene is null");
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
        HideWeapon(weapon);

        if (weaponType == SelectedSlot)
        {
            SelectWeapon(weaponType, true);
        }

        GD.Print($"Added weapon of type {weaponType}");
    }

    public void SelectWeapon(WeaponData.WeaponType weaponTypeToSelect, bool newWeapon = false)
    {
        // if trying to select weapon in already selected slot, do nothing.
        if (weaponTypeToSelect == SelectedSlot)
        {
            if (WeaponSlots[weaponTypeToSelect] != null && !newWeapon)
            {
                GD.Print($"Can't select weapon of type {weaponTypeToSelect}: Already selected");
                return;
            }
        }

        if (WeaponSlots.TryGetValue(weaponTypeToSelect, out Weapon weapon))
        {
            if (IsInstanceValid(weapon))
            {
                Weapon currentWeapon = WeaponSlots[SelectedSlot];
                HideWeapon(currentWeapon);
                ShowWeapon(weapon);

                SelectedSlot = weaponTypeToSelect;

                // Equip animation
                Position = new Vector3(0.095f, -0.11f - 0.15f, -0.12f - -0.15f);
            }
            else
            {
                GD.Print($"Can't select weapon of type {weaponTypeToSelect}: Weapon is null");
            }
        }
        else
        {
            GD.Print($"Can't select weapon of type {weaponTypeToSelect}: No such slot is present");
        }

        GD.Print($"Selected weapon of type {weaponTypeToSelect}");
    }

    public void HideWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            GD.Print("Can't hide weapon: Weapon is null");
            return;
        }

        weapon.IsAiming = false;

        weapon.Visible = false;
        weapon.SetProcess(false);
    }

    public void ShowWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            GD.Print("Can't show weapon: Weapon is null");
            return;
        }

        weapon.Visible = true;
        weapon.SetProcess(true);
    }
}