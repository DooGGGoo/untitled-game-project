using Godot;

[GlobalClass]
public partial class PropData : Resource
{
    [Export] public PhysicsProp.PropMaterialType MaterialType;
    [Export] public float ImpactTriggerVelocity = 0.4f;
    [Export] public int GibsCount = 0;
    [Export] public int ExplosiveDamage = 0;
    [Export] public float ExplosiveRadius = 0;
}