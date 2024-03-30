using Godot;

public partial class Weapon : Node3D
{
    [Export] public WeaponData WeaponData;
    [Export] public RayCast3D RayCast;
    [Export] public FireMode WeaponFireMode;

    public bool IsAiming;

    private float recoilCurvePosition = 0f;
    private Vector3 currentRotation, currentPosition;
    private Vector3 recoilTargetRotation, recoilTargetPosition; 

    public int CurrentAmmo;
    private float timeSinceLastShot;
    private bool isReloading;
    public enum FireMode { Semi, Auto } 

    [Signal] public delegate void ShotFiredEventHandler();

    public override void _Ready()
    {
        CurrentAmmo = WeaponData.MaxAmmo;
    }

    public override void _Process(double delta)
    {
        if (WeaponFireMode == FireMode.Auto && Input.IsActionPressed("left_click"))
        {
            PrimaryAttack();
        }
        else if (WeaponFireMode == FireMode.Semi && Input.IsActionJustPressed("left_click"))
        {
            PrimaryAttack();
        }

        if (Input.IsActionJustPressed("right_click"))
        {
            IsAiming = !IsAiming;
        }

        if (Input.IsActionJustPressed("reload"))
        {
            Reload();
        }

        if (Input.IsActionJustPressed("change_firemode"))
        {
            WeaponFireMode = WeaponFireMode == FireMode.Auto ? FireMode.Semi : FireMode.Auto;
        }

        timeSinceLastShot += (float)delta;
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessRecoil(delta);
    }

    public virtual bool IsUseable()
    {
        return CurrentAmmo > 0 && timeSinceLastShot >= WeaponData.FireRate && !isReloading;
    }

    public virtual void PrimaryAttack()
    {
        if (!IsUseable()) return;

        CurrentAmmo--;
        timeSinceLastShot = 0f;

        AddRecoil(WeaponData.RecoilCurveSpeed);

        EmitSignal(SignalName.ShotFired);

        if (RayCast.GetCollider() is not Node target)
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
        StandardMaterial3D MaterialOverride = new()

        {
            AlbedoColor = new Color(1, 0, 0),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        MeshInstance3D debugMesh = new()

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
    }

    public virtual void SecondaryAttack() { }

    public async virtual void Reload()
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
        if (IsAiming)
        {
            recoilTargetPosition = recoilTargetPosition.Lerp(Vector3.Zero - GetParentNode3D().Position + new Vector3(0f, WeaponData.AimOffsetY, WeaponData.AimOffsetZ), WeaponData.RecoilReturnRate);
        }
        else
        {
            recoilTargetPosition = recoilTargetPosition.Lerp(Vector3.Zero, WeaponData.RecoilReturnRate);
        }
        // Apply recoil to final rotation
        currentRotation = currentRotation.Lerp(recoilTargetRotation, WeaponData.RecoilHarshness);
        currentPosition = currentPosition.Lerp(recoilTargetPosition, WeaponData.RecoilHarshness);

        Position = currentPosition;
        Rotation = currentRotation;
    }

    protected virtual void AddRecoil(float amount)
    {
        recoilMagnitude = Mathf.Clamp(recoilMagnitude + amount, 0f, 1f);

        float recoilUp = WeaponData.RecoilForceUp * recoilCurveValue + (float)GD.RandRange(-WeaponData.RecoilForceUp / 8f, WeaponData.RecoilForceUp / 8f);
        float recoilSide = (float)GD.RandRange(-WeaponData.RecoilDispersion, WeaponData.RecoilDispersion) * (1 - recoilCurveValue);

        Vector3 recoilDirection = new(recoilUp, recoilSide, 0f);

        recoilTargetRotation += recoilDirection;
        recoilTargetPosition.Z += WeaponData.RecoilForceBack;


        Global.Instance.CurrentLevel.CurrentPlayer.PlayerView.AddCameraShake(0.12f * (1f - recoilCurveValue));
        Global.Instance.CurrentLevel.CurrentPlayer.PlayerView.ViewPunch(recoilDirection * WeaponData.CameraRecoil / 1.2f, true);

        //GD.Print(recoilMagnitude, recoilCurveValue);
    }

    protected float GetRecoilMagnitude()
    {
        return recoilMagnitude * recoilMagnitude;
    }
}