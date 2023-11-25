using Godot;
using System;
using System.Linq;

public partial class SCP173 : CharacterBody3D
{
	[Export] private NavigationAgent3D navigationAgent;
	[Export] private VisibleOnScreenNotifier3D onScreenNotifier;
	[Export] private Timer cooldownTimer;

	private Player player;

	private const float gravity = 16.8f;
	private const float speed = 20f;


	public override void _Ready()
	{
		player = GetTree().GetNodesInGroup("Player").OfType<Player>().OrderBy(p => p.GlobalPosition.DistanceTo(GlobalPosition)).FirstOrDefault();
	}
	

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		navigationAgent.TargetPosition = new Vector3(
			(player.GlobalPosition - -(GlobalPosition - player.GlobalPosition).Normalized()).X,
			player.GlobalPosition.Y,
			(player.GlobalPosition - -(GlobalPosition - player.GlobalPosition).Normalized()).Z
			);

		Vector3 targetPosition = navigationAgent.GetNextPathPosition();
		Vector3 navDirection = targetPosition - GlobalPosition;
		Vector3 wishDir = navDirection.Normalized();

		if (!onScreenNotifier.IsOnScreen())
		{
			velocity.X =  wishDir.X * speed;
			velocity.Z = wishDir.Z * speed;

			LookAt(new Vector3(player.GlobalPosition.X, GlobalPosition.Y, player.GlobalPosition.Z));
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;
		}

		Velocity = velocity;

		var collision = MoveAndCollide(Velocity * (float)delta, true);

		if (collision != null && collision.GetCollider() is RigidBody3D rigidbody)
		{
			var pushVector = collision.GetNormal() * (Velocity.Length() * 2f / rigidbody.Mass);
			rigidbody.ApplyImpulse(-pushVector, collision.GetPosition() - rigidbody.GlobalPosition);
		}

		MoveAndSlide();
	}
}
