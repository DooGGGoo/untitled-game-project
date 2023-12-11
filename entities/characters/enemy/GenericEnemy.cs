using Godot;
using System;
using System.Linq;

public partial class GenericEnemy : GroundCharacter, IDamageableEntity, IInteractable
{
	[Export] private NavigationAgent3D navigationAgent;
    [Export] public int MaxHealth { get; set; } = 50;
    
	public int Health { get; set; }
	private Player player;
	private Vector3 avoidedVelocity;

	public override void _Ready()
	{
		Health = MaxHealth;

		navigationAgent.VelocityComputed += (Vector3 velocity) => 
		{
			if (velocity != Vector3.Zero) 
			{
				Velocity = velocity;
			}
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		CalculateMoveDirection();
		GetNodeOrNull<MeshInstance3D>("MeshInstance3D").LookAt(new Vector3(player.GlobalPosition.X, GlobalPosition.Y, player.GlobalPosition.Z));
		base._PhysicsProcess(delta);
	}

	protected override void ProcessMovement(double delta)
    {
		Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        if (wishDir != Vector3.Zero && !navigationAgent.IsTargetReached())
        {
            MovementAcceleration = IsOnFloor() ? 9f : 1f;
            velocity.X = Mathf.Lerp(velocity.X, wishDir.X * currentSpeed, MovementAcceleration * (float)delta);
            velocity.Z = Mathf.Lerp(velocity.Z, wishDir.Z * currentSpeed, MovementAcceleration * (float)delta);
        }
        else
        {
            MovementFriction = IsOnFloor() ? 12f : 1f;
            velocity.X = Mathf.Lerp(velocity.X, 0f, MovementFriction * (float)delta);
            velocity.Z = Mathf.Lerp(velocity.Z, 0f, MovementFriction * (float)delta);
        }

		Velocity = velocity;
		navigationAgent.Velocity = Velocity;
    }

	public void CalculateMoveDirection()
	{
		if (player == null)
		{
			player = GetTree().GetNodesInGroup("Player").OfType<Player>().OrderBy(p => p.GlobalPosition.DistanceTo(GlobalPosition)).FirstOrDefault();
			return;
		}

		navigationAgent.TargetPosition = new Vector3(
			(player.GlobalPosition - -(GlobalPosition - player.GlobalPosition).Normalized() * 1.75f).X,
			player.GlobalPosition.Y,
			(player.GlobalPosition - -(GlobalPosition - player.GlobalPosition).Normalized() * 1.75f).Z
			);

		Vector3 targetPosition = navigationAgent.GetNextPathPosition();
		Vector3 navDirection = targetPosition - GlobalPosition;
		wishDir = navDirection.Normalized();
	}

	#region Interactions

	public void Interact(CharacterBody3D interactor)
    {
        int damage = Random.Shared.Next(1, 10);
		TakeDamage(damage);
		Push(damage + 2, -(interactor.GlobalPosition - GlobalPosition) + Vector3.Up, 3);
    }

	public void Heal(int amount)
	{
		Health += amount;
		if (Health > MaxHealth)
			Health = MaxHealth;
	}

	public void TakeDamage(int damage)
	{
		Health -= damage;
		GD.Print("Taking damage: " + damage + " | Health: " + Health);
		Push(damage + 2, GlobalTransform.Basis.Z + Vector3.Up, 3);
		if (Health <= 0)
			Kill();
	}

	public void Kill()
    {
		GD.Print("Bro I'm dead 💀💀💀💀");
        QueueFree();
    }

	#endregion

	public void Push(float force, Vector3 dir, float mass)
	{
		Velocity += force * dir / mass;
	}

}
