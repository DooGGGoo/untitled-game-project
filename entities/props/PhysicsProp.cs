using Godot;

[GlobalClass]
public partial class PhysicsProp : RigidBody3D
{
    [Export] private PropData propData = new();
    private Vector3 collisionPosition;

    public enum PropMaterialType {Stone, Wooden, Metal}

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 1;

        BodyEntered += (Node node) => 
        {
            if (Mathf.Abs(LinearVelocity.Length()) >= propData.ImpactTriggerVelocity)
                OnImpact(node);
        };
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (state.GetContactCount() > 0 && Mathf.Abs(LinearVelocity.Length()) >= propData.ImpactTriggerVelocity)
        {
            collisionPosition = state.GetContactColliderPosition(0);
        }
    }

    public virtual void OnImpact(Node node)
    {
        if (node is not Node3D) return;

        GD.Print("Impact on " + collisionPosition);

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
                Radius = 0.1f,
                Height = 0.1f,
            },
            MaterialOverride = MaterialOverride,
            GlobalPosition = collisionPosition
        };

        GetParent().AddChild(debugMesh);
    }
}