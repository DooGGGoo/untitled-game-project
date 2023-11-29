using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.5f;
	[Export] private bool isNoclip = false;
	[Export] private CollisionShape3D StandingCollisionShape, CrouchingCollisionShape;
	[Export] private ShapeCast3D CrouchAboveCheck;
	[Export] private RayCast3D InteractionCheck;
	[Export] private Node3D GrabbedObjectPositionMarker;
	[Export] private float Bob = 0.15f;
	[Export] private float bobUp = 0.5f;
	[Export] private float bobCycle = 0.8f;

	// Movement
	private float MovementAcceleration = 6f;
	private float MovementFriction = 8f;
	private float currentSpeed;
	private const float crouchDepth = -0.8f;
	public const float WalkSpeed = 4f;
	public const float SprintSpeed = 5.8f;
	public const float CrouchSpeed = 2.3f;
	public const float JumpVelocity = 1.2f;
	private const float gravity = 16.8f;
	private const float StepHeight = 0.25f;

	// Camera
	private Vector3 cameraTargetRotation;
	[Export] private float cameraRollAngle = .5f;
	[Export] private float cameraRollSpeed = 3f;
	private float bobTime = 0f;
	private float bobFinal;
	// Grabbing
	private const float grabObjectPullPower = 22f;
	private RigidBody3D grabbedObject;

    public override void _Ready()
    {
		Input.MouseMode = Input.MouseModeEnum.Captured;

		base._Ready();
    }

    public override void _Input(InputEvent @event)
    {
		if (@event.IsActionPressed("use"))
		{
			InteractWithObject();
		}

		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseMotionEvent = @event as InputEventMouseMotion;
			cameraTargetRotation.X -= mouseMotionEvent.Relative.Y * Sensitivity;
			cameraTargetRotation.X = Mathf.Clamp(cameraTargetRotation.X, -89.9f, 89.9f);
			cameraTargetRotation.Y -= mouseMotionEvent.Relative.X * Sensitivity;
		}

		if (@event.IsActionPressed("jump"))
		{
			Jump();
		}

		if (@event.IsActionPressed("debug_noclip"))
		{
			isNoclip = !isNoclip;
			StandingCollisionShape.Disabled = isNoclip;
			CrouchingCollisionShape.Disabled = isNoclip;
		}
    
		if (Input.IsActionJustPressed("use"))
		{
			if (grabbedObject == null)
			{
				GrabObject();
			}
			else
			{
				DropObject();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isNoclip == true)
		{
			ProcessMovementNoclip(delta);
		}
		else
		{
			ProcessMovement(delta);
		}

		ProcessCameraMovement(delta);

		ProcessGrabbedObject();
		PushRigidBodies(delta);
		MoveAndSlide();
	}

	#region Camera
	private void ProcessCameraMovement(double delta)
	{
		// HACK
		float cameraZRotation = cameraTargetRotation.Z;
		// Camera roll
		// ------------------------------------
		float sign, side, angle;

		side = Velocity.Dot(-PlayerCamera.GlobalBasis.X);
		sign = Mathf.Sign(side);
		side = Mathf.Abs(side);
		angle = cameraRollAngle;
		if (side < cameraRollSpeed)
		{
			side = side * angle / cameraRollSpeed;
		}
		else
		{
			side = angle;
		}

		cameraZRotation = side * sign;

		// Camera bob
		// ------------------------------------
		if (Velocity.Length() <= 0.1f)
		{
			bobTime = 0f;
		}
		else
		{
			bobFinal = CalculateBob(1f, bobFinal, delta);
		}
		cameraZRotation += bobFinal * .8f;
		cameraTargetRotation.Y -= bobFinal * .8f;
		cameraTargetRotation.X += bobFinal * 1.2f;

		// Apply all transformations, including mouse movement
		// ------------------------------------
		PlayerCamera.RotationDegrees = new Vector3(cameraTargetRotation.X, cameraTargetRotation.Y, cameraZRotation);
	}

	private float CalculateBob(float freq, float bob, double delta)
	{
		float cycle;

		if (isNoclip) return 0f;

		if (!IsOnFloor()) return bob;

		bobTime += (float)delta * freq;
		cycle = bobTime - (int)(bobTime / bobCycle) * bobCycle;
		cycle /= bobCycle;

		if (cycle < bobUp)
			cycle = Mathf.Pi * cycle / bobUp;
		else
			cycle = Mathf.Pi + Mathf.Pi * (cycle - bobUp) / (1f - bobUp);

		bob = Mathf.Sqrt(Velocity.X * Velocity.X + Velocity.Z * Velocity.Z) * Bob;
		bob = bob * 0.3f + bob * 0.7f * Mathf.Cos(cycle);
		bob = Mathf.Clamp(bob, -7f, 4f);

		return bob;
	}

	#endregion

	#region Movement
	private void ProcessMovement(double delta)
	{
		Vector3 velocity = Velocity;

		if (Input.IsActionPressed("crouch"))
		{
			currentSpeed = CrouchSpeed;
			PlayerCamera.Position = new Vector3(PlayerCamera.Position.X, Mathf.Lerp(PlayerCamera.Position.Y, 1.5f + crouchDepth, 7f * (float)delta), PlayerCamera.Position.Z);
			if (StandingCollisionShape.Disabled == false)
			{
				StandingCollisionShape.Disabled = true;
				CrouchingCollisionShape.Disabled = false;
			}
		}

		else if (!CrouchAboveCheck.IsColliding())
		{
			if (StandingCollisionShape.Disabled == true)
			{
				StandingCollisionShape.Disabled = false;
				CrouchingCollisionShape.Disabled = true;
			}

			PlayerCamera.Position = new Vector3(PlayerCamera.Position.X, Mathf.Lerp(PlayerCamera.Position.Y, 1.5f, 7f * (float)delta), PlayerCamera.Position.Z);
			if (Input.IsActionPressed("sprint"))
			{
				currentSpeed = SprintSpeed;
			}
			else
			{
				currentSpeed = WalkSpeed;
			}
		}

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 wishDirRaw = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();
		Vector3 wishDir = (Transform.Basis * new Vector3(wishDirRaw.X, 0, wishDirRaw.Z)).Normalized();

		if (wishDir != Vector3.Zero)
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
	}

	private void ProcessMovementNoclip(double delta)
	{
		Vector3 velocity = Velocity;

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 wishDir = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();

		if (wishDir != Vector3.Zero)
			velocity = velocity.Lerp(wishDir * currentSpeed * 2f, 9f * (float)delta);
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

	private void PushRigidBodies(double delta)
	{
		var collision = MoveAndCollide(Velocity * (float)delta, true);

		if (collision != null && collision.GetCollider() is RigidBody3D rigidbody)
		{
			var pushVector = collision.GetNormal() * (Velocity.Length() * 2f / rigidbody.Mass);
			rigidbody.ApplyImpulse(-pushVector, collision.GetPosition() - rigidbody.GlobalPosition);
		}
	}
	

	#endregion

	#region Object Interactions
	private void InteractWithObject()
	{
		if (InteractionCheck != null && InteractionCheck.IsColliding())
		{
            if (InteractionCheck.GetCollider() is Node collidedObject && collidedObject is IInteractable interactable)
            {
                interactable.Interact(this);
            }
        }
	}
	
	private void GrabObject()
	{
		if (InteractionCheck != null && InteractionCheck.IsColliding())
		{
            if (InteractionCheck.GetCollider() is Node collidedObject && collidedObject is RigidBody3D)
            {
                grabbedObject = collidedObject as RigidBody3D;
            }
        }
	}

	private void DropObject()
	{
		if (grabbedObject != null)
		{
			grabbedObject = null;
		}
	}

	private void ProcessGrabbedObject()
	{
		if (grabbedObject != null)
		{
			Vector3 objPosition = grabbedObject.GlobalPosition;
			Vector3 targetPosition = GrabbedObjectPositionMarker.GlobalPosition;

			grabbedObject.LinearVelocity = (targetPosition - objPosition) * grabObjectPullPower;
			grabbedObject.Rotation = PlayerCamera.Rotation;

			if (InteractionCheck.GetCollider() != grabbedObject && InteractionCheck.GetCollider() != null)
			{
				DropObject();
			}
		}
	}
	#endregion
}
