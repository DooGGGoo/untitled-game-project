using System.Linq;
using Godot;

[GlobalClass]
public partial class CameraShakeTrigger : Area3D
{
    [Export] public float ShakeAmount = 0.1f;
    [Export] public bool AlwaysProcess = false;

    public override void _PhysicsProcess(double delta)
    {
        if (AlwaysProcess)
        {
            AddShake();
        }
    }

    public void AddShake()
    {
        Player player = GetTree().GetNodesInGroup("Player").OfType<Player>().OrderBy(p => p.GlobalPosition.DistanceTo(GlobalPosition)).FirstOrDefault();

        if (OverlapsBody(player))
        {
            player.PlayerView.AddCameraShake(ShakeAmount);
        }
    }
}