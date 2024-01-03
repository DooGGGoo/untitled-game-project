using Godot;

[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public int Damage = 5;
    [Export] public int MaxAmmo = 10;
    [Export] public float FireRate = 0.5f;
    [Export] public float ReloadTime = 1.0f;
    [Export] public WeaponType Type = WeaponType.Primary;
    [ExportGroup("Recoil")]
    [Export] public Curve RecoilCurve;
    [Export] public float RecoilForceUp = 0.4f;
    [Export] public float RecoilForceBack = 0.02f;
    [Export] public float RecoilDispersion = 0.15f;
    [Export] public float CameraRecoil = 0.25f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RecoilReturnRate = 1.0f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RecoilHarshness = 1.0f;
    [Export] public float RecoilCurveSpeed = 0.2f;
    [Export] public float RecoilDecreaseSpeed = 0.4f;
    [ExportGroup("Aiming")]
    [Export] public float AimOffsetY, AimOffsetZ;

    public enum WeaponType
    {
        Melee,
        Primary,
        Secondary
    }
}