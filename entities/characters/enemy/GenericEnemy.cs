using Godot;
using System;
using System.Linq;

public partial class GenericEnemy : CharacterBody3D, ILivingEntity, IInteractable
{
	private const float gravity = 16.8f;
	public const float Speed = 3f;

    public int Health { get; set; }
    public int MaxHealth { get; set; } = 50;

	private Vector3 wishDir;

	public override void _Ready()
	{
		Health = MaxHealth;
	}

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

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		CalculateMoveDirection();

		if (wishDir != Vector3.Zero)
		{
			float MovementAcceleration = IsOnFloor() ? 9f : 1f;
			velocity.X = Mathf.Lerp(velocity.X, wishDir.X * Speed, MovementAcceleration * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, wishDir.Z * Speed, MovementAcceleration * (float)delta);
		}
		else
		{
			float MovementFriction = IsOnFloor() ? 12f : 1f;
			velocity.X = Mathf.Lerp(velocity.X, 0f, MovementFriction * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, 0f, MovementFriction * (float)delta);
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

	public void Push(float force, Vector3 dir, float mass)
	{
		Velocity += force * dir / mass;
	}

	private void CalculateMoveDirection()
	{
		// Calculate Direction to nearest player and move towards them, until distance is close enough
		Player player = GetTree().GetNodesInGroup("Player").OfType<Player>().OrderBy(p => p.GlobalPosition.DistanceTo(GlobalPosition)).FirstOrDefault();
		if (player != null)
		{
			Vector3 navDestination = player.GlobalPosition;
			Vector3 navDirection = navDestination - GlobalPosition;
			wishDir = navDirection.Normalized();
		

			if (navDirection.Length() < 2f)
				wishDir = Vector3.Zero;
			else
				LookAt(new Vector3(navDestination.X, GlobalPosition.Y, navDestination.Z));
		}
	} 
}
