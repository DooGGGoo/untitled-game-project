using Godot;

[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public int Damage = 5;
    [Export] public int MaxAmmo = 10;
    [Export] public float FireRate = 0.5f;
    [Export] public float ReloadTime = 1.0f;
    [Export] public Curve RecoilCurve;
}