using System;
using Godot;

public partial class Weapon : Node3D
{
    [Export] public WeaponData WeaponData;
    [Export] public RayCast3D RayCast;
    [Export] public FireMode WeaponFireMode;

    private float recoilCurvePosition = 0f;
    private Vector3 currentRotation, recoilTargetRotation;

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
        if (Input.IsActionPressed("left_click"))
        {
            PrimaryAttack();
        }

        timeSinceLastShot += (float)delta;
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessRecoil(delta);
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

        AddRecoil(WeaponData.RecoilCurveSpeed);

        if (target == null)
        {
            return;
        }

        if (target is IDamageableEntity entity)
        {
            entity.TakeDamage(WeaponData.Damage);
        }

        if (target is RigidBody3D rigidBody)
        {
            rigidBody.ApplyCentralImpulse(-RayCast.GetCollisionNormal() * WeaponData.Damage * 4f / rigidBody.Mass);
        }

        OnHit(RayCast.GetCollisionPoint());
    }

    protected virtual void OnHit(Vector3 hitPosition)
    {
        // DEBUG //

        // create new debug mesh at collision point
        StandardMaterial3D MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(1, 0, 0),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        MeshInstance3D debugMesh = new MeshInstance3D
        {
            Mesh = new SphereMesh()
            {
                Rings = 4,
                RadialSegments = 8,
                Radius = 0.025f,
                Height = 0.05f,
            },
            MaterialOverride = MaterialOverride,
            TopLevel = true
        };

        GetParent().AddChild(debugMesh);

        debugMesh.GlobalPosition = hitPosition;

        // DEBUG //
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

    private float recoilMagnitude, recoilCurveValue;

    protected virtual void ProcessRecoil(double delta)
    {
        recoilMagnitude = Mathf.Max(recoilMagnitude - (float)delta * WeaponData.RecoilDecreaseSpeed, 0f);

        recoilCurveValue = WeaponData.RecoilCurve.Sample(GetRecoilMagnitude());

        // Reset position and rotation over time
        //
        // This is NOT the final value of the rotation, it's calculated value that rotation (currentRotation) will lerp to
        recoilTargetRotation = recoilTargetRotation.Lerp(Vector3.Zero, WeaponData.RecoilReturnRate);

        // Apply recoil to final rotation
        currentRotation = currentRotation.Lerp(recoilTargetRotation, WeaponData.RecoilHarshness);

        Rotation = currentRotation;
    }

    protected virtual void AddRecoil(float amount)
    {
        recoilMagnitude = Mathf.Clamp(recoilMagnitude + amount, 0f, 1f);

        recoilTargetRotation += new Vector3(
            WeaponData.RecoilAmount.X  * recoilCurveValue + (float)GD.RandRange(-WeaponData.RecoilAmount.X / 8f, WeaponData.RecoilAmount.X / 8f),
            (float)GD.RandRange(-WeaponData.RecoilAmount.Y, WeaponData.RecoilAmount.Y),
            (float)GD.RandRange(-WeaponData.RecoilAmount.Z, WeaponData.RecoilAmount.Z)
        );
        GD.Print(recoilMagnitude, recoilCurveValue);
    }

    protected float GetRecoilMagnitude()
    {
        return recoilMagnitude * recoilMagnitude;
    }
}