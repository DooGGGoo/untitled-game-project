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

	private Vector2 mouseMotion;
	private float MovementAcceleration = 6f;
	private float MovementFriction = 8f;
	private float crouchDepth = -0.8f;

	private float currentSpeed;
	public const float WalkSpeed = 4f;
	public const float SprintSpeed = 5.8f;
	public const float CrouchSpeed = 2.3f;
	public const float JumpVelocity = 1.2f;
	private const float gravity = 16.8f;

	private const float grabObjectPullPower = 22f;
	private RigidBody3D grabbedObject;


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
			mouseMotion.X = Mathf.Clamp(mouseMotion.X, -89.9f, 89.9f);
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
			StandingCollisionShape.Disabled = isNoclip;
			CrouchingCollisionShape.Disabled = isNoclip;
		}
    
		// TODO: Change input action to something else
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
		PlayerCamera.RotationDegrees = new Vector3(mouseMotion.X, mouseMotion.Y, PlayerCamera.RotationDegrees.Z);
		
		if (isNoclip == true)
		{
			ProcessMovementNoclip(delta);
		}
		else
		{
			ProcessMovement(delta);
		}

		// Rotate grabObjectCameraOffset to match with the rotation of the camera
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

		PushRigidBodies(delta);
		MoveAndSlide();
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


	private void InteractWithObject()
	{
		if (InteractionCheck != null && InteractionCheck.IsColliding())
		{
			Node collidedObject = InteractionCheck.GetCollider() as Node;
			if (collidedObject != null && collidedObject is IInteractable)
			{
				((IInteractable)collidedObject).Interact(this);
			}
		}
	}
	

	private void GrabObject()
	{
		if (InteractionCheck != null && InteractionCheck.IsColliding())
		{
			Node collidedObject = InteractionCheck.GetCollider() as Node;
			if (collidedObject != null && collidedObject is RigidBody3D)
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

}
