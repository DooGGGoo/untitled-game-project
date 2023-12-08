using Godot;

public partial class Weapon : Node3D
{
    [Export] public WeaponData WeaponData;
    [Export] public RayCast3D RayCast;
    [Export] public FireMode WeaponFireMode;

    public int CurrentAmmo;
    private float timeSinceLastShot;
    private bool isReloading;
    public enum FireMode { Semi, Auto } 

    public override void _Ready()
    {
        CurrentAmmo = WeaponData.MaxAmmo;
    }

    public override void _Process(double delta)
    {
        timeSinceLastShot += (float)delta;
    }

    public bool IsUseable()
    {
        return CurrentAmmo > 0 && timeSinceLastShot >= WeaponData.FireRate && !isReloading;
    }

    public virtual void PrimaryAttack()
    {
        if (!IsUseable()) return;

        CurrentAmmo--;
        timeSinceLastShot = 0f;

        Node target = RayCast.GetCollider() as Node;

        if (target == null)
        {
            GD.Print("No target");
            return;
        }

        if (target is ILivingEntity entity)
        {
            entity.TakeDamage(WeaponData.Damage);
        }

        if (target is RigidBody3D rigidBody)
        {
            rigidBody.ApplyCentralImpulse(-RayCast.GetCollisionNormal() * WeaponData.Damage * 4f / rigidBody.Mass);
        }
    }

    public virtual void SecondaryAttack() { }

    public async void Reload()
    {
        isReloading = true;

        await ToSignal(GetTree().CreateTimer(WeaponData.ReloadTime), SceneTreeTimer.SignalName.Timeout);

        if(CurrentAmmo > 0)
			CurrentAmmo = WeaponData.MaxAmmo + 1;
		else
			CurrentAmmo = WeaponData.MaxAmmo;

        isReloading = false;
    }
}