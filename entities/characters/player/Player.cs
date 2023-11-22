using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.5f; 
	[Export] private bool isNoclip = false;

	private Vector2 mouseMotion;
	private float MovementAcceleration = 6f;
	private float MovementFriction = 8f;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 1.2f;
	public const float gravity = 16.8f;


    [Signal]
    public delegate void ObjectInteractedEventHandler();

    public override void _Ready()
    {
		Input.MouseMode = Input.MouseModeEnum.Captured;
        base._Ready();
    }


    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseMotionEvent = @event as InputEventMouseMotion;
			mouseMotion.X -= mouseMotionEvent.Relative.Y * Sensitivity;
			mouseMotion.X = Mathf.Clamp(mouseMotion.X, -90f, 90f);
			mouseMotion.Y -= mouseMotionEvent.Relative.X * Sensitivity; 
		}
		if (@event.IsActionPressed("use"))
		{
			InteractWithObject();
		}
		if (@event.IsActionPressed("jump"))
		{
			Jump();
		}
		if (@event.IsActionPressed("debug_noclip"))
		{
			isNoclip = !isNoclip;
			GetNode<CollisionShape3D>("CollisionShape3D").Disabled = isNoclip;
		}
        base._Input(@event);
    }


    public override void _PhysicsProcess(double delta)
	{
		PlayerCamera.RotationDegrees = new Vector3(mouseMotion.X, mouseMotion.Y, PlayerCamera.RotationDegrees.Z);
		
		if (isNoclip == true)
		{
			ProcessMovementNoclip(delta);
		}
		else
		{
			ProcessMovement(delta);
		}


		var collision = MoveAndCollide(Velocity * (float)delta, true);

		if (collision != null && collision.GetCollider() is RigidBody3D rigidbody)
		{
			var pushVector = collision.GetNormal() * (Velocity.Length() * 2f / rigidbody.Mass);
			rigidbody.ApplyImpulse(-pushVector, collision.GetPosition() - rigidbody.GlobalPosition);
		}

		MoveAndSlide();
	}


	private void ProcessMovement(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 wishDirRaw = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();
		Vector3 wishDir = (Transform.Basis * new Vector3(wishDirRaw.X, 0, wishDirRaw.Z)).Normalized();

		if (wishDir != Vector3.Zero)
		{
			MovementAcceleration = IsOnFloor() ? 9f : 1f;
			velocity.X = Mathf.Lerp(velocity.X, wishDir.X * Speed, MovementAcceleration * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, wishDir.Z * Speed, MovementAcceleration * (float)delta);
		}
		else
		{
			MovementFriction = IsOnFloor() ? 12f : 1f;
			velocity.X = Mathf.Lerp(velocity.X, 0f, MovementFriction * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, 0f, MovementFriction * (float)delta);
		}

		Velocity = velocity;
	}


	private void ProcessMovementNoclip(double delta)
	{
		Vector3 velocity = Velocity;

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 wishDir = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();

		if (wishDir != Vector3.Zero)
			velocity = velocity.Lerp(wishDir * Speed * 2f, 9f * (float)delta);
		else
			velocity = velocity.Lerp(Vector3.Zero, 12f * (float)delta);

		Velocity = velocity;
	}


	public void Jump()
	{
		if (!IsOnFloor()) return;
		float jMult = Mathf.Sqrt(2f * gravity * JumpVelocity);
		Vector3 vel = Velocity;
		vel.Y += jMult;
		Velocity = vel;
	}


	private void InteractWithObject()
	{
		RayCast3D raycast = GetNodeOrNull<RayCast3D>("Camera3D/RayCast3D");
		if (raycast != null && raycast.IsColliding())
		{
			Node collidedObject = raycast.GetCollider() as Node;
			if (collidedObject != null && collidedObject is IInteractable)
			{
				((IInteractable)collidedObject).Interact(this);
			}
		}
	}
}
