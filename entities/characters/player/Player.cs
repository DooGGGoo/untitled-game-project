using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.5f;
	[Export] private bool isNoclip = false;
	[Export] private CollisionShape3D StandingCollisionShape, CrouchingCollisionShape;
	[Export] private ShapeCast3D CrouchAboveCheck;
	[Export] private RayCast3D InteractionCheck;
	[Export] private Node3D GrabbedObjectPositionMarker;

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
	private const float StepHeight = 0.45f;

	[ExportGroup("Camera")]
	[Export] private float cameraRollAngle = .5f;
	[Export] private float cameraRollSpeed = 3f;
	[Export] public bool enableHeadbob = true;
	[Export] private float headbobTimer = 10f;  // Speed
	[Export] private float headbobScale = 0.1f; // Magnitude
	private float time;
	private Vector3 cameraTargetRotation;
	private Vector3 oldPosition;

	[ExportGroup("Object interactions")]
	[Export] private Generic6DofJoint3D grabJoint;
	[Export] private StaticBody3D grabStaticBody;
	private const float grabObjectPullPower = 22f;
	private const float grabRotationPower = 0.5f;
	private bool grabMouseLock;
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

		if (@event is InputEventMouseMotion && !grabMouseLock)
		{
			InputEventMouseMotion mouseMotionEvent = @event as InputEventMouseMotion;
			cameraTargetRotation.X -= mouseMotionEvent.Relative.Y * Sensitivity;
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

		if (Input.IsActionPressed("left_click"))
		{
			grabMouseLock = true;
			RotateObject(@event);
		}
		if (Input.IsActionJustReleased("left_click"))
		{
			grabMouseLock = false;
		}

		if (Input.IsActionJustPressed("right_click"))
		{
			if (grabbedObject != null)
			{
				Vector3 knockback = grabbedObject.GlobalPosition - GlobalPosition;
				grabbedObject.ApplyCentralImpulse(knockback * 10f);
				DropObject();
			}
		}
	}

	bool StartedProcessOnFloor = false;
	public override void _PhysicsProcess(double delta)
	{
		time += (float)delta;

		if (isNoclip == true)
		{
			ProcessMovementNoclip(delta);
		}
		else
		{
			ProcessMovement(delta);
		}

		oldPosition = PlayerCamera.GlobalPosition;

		ProcessGrabbedObject();
		PushRigidBodies(delta);
		StartedProcessOnFloor = IsOnFloor();
		MoveAndClimbStairs((float)delta, false);
		ProcessCameraMovement(delta);
		ProcessViewmodel();
	}

	#region Camera
	private void ProcessCameraMovement(double delta)
	{
		// Smooth camera when moving up stairs
		if (IsOnFloor() && PlayerCamera.GlobalPosition.Y - oldPosition.Y > 0)
		{
			float stepTime = (float)delta;

			oldPosition.Y += stepTime * 0.5f;

			if (oldPosition.Y > PlayerCamera.GlobalPosition.Y)
				oldPosition.Y = PlayerCamera.GlobalPosition.Y;

			if (PlayerCamera.GlobalPosition.Y - oldPosition.Y > 0.14f)
				oldPosition.Y = PlayerCamera.GlobalPosition.Y - 0.14f;
			
			Vector3 pos = PlayerCamera.GlobalPosition;
			pos.Y += oldPosition.Y - PlayerCamera.GlobalPosition.Y;
			PlayerCamera.GlobalPosition = pos;
		}
		else
			oldPosition.Y = PlayerCamera.GlobalPosition.Y;


		// Camera roll
		float sign, side, angle;

		side = Velocity.Dot(-PlayerCamera.GlobalBasis.X);
		sign = Mathf.Sign(side);
		side = Mathf.Abs(side);
		angle = cameraRollAngle;

		if (side < cameraRollSpeed)
			side = side * angle / cameraRollSpeed;
		else
			side = angle;

        float cameraZRotation = side * sign;

		// Camera bob
		if (enableHeadbob == true && IsOnFloor())
		{
			Vector2 offset;

			offset.Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(Velocity.Length()) * headbobScale / 10f;
			offset.X = Mathf.Cos(2f * time * headbobTimer) * Mathf.Abs(Velocity.Length()) * headbobScale / 40f;

			cameraTargetRotation.Y += offset.Y;
			cameraTargetRotation.X += offset.X;
		}

		// Apply all rotation changes
		cameraTargetRotation.X = Mathf.Clamp(cameraTargetRotation.X, -89.9f, 89.9f);
		PlayerCamera.RotationDegrees = new Vector3(cameraTargetRotation.X, cameraTargetRotation.Y, cameraZRotation);
	}

	private void ProcessViewmodel()
	{
		Node3D viewmodel = GetNode<Node3D>("%Viewmodel");


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
		KinematicCollision3D collision = MoveAndCollide(Velocity * (float)delta, true);

		if (collision != null && collision.GetCollider() is RigidBody3D rigidbody)
		{
			Vector3 pushVector = collision.GetNormal() * (Velocity.Length() * 2f / rigidbody.Mass);
			rigidbody.ApplyImpulse(-pushVector, collision.GetPosition() - rigidbody.GlobalPosition);
		}
	}

	#region Step handling code, Do not Touch, Breathe or Stare at
	// Source: (https://github.com/wareya/GodotStairTester/blob/main/player/SimplePlayer.gd), Huge thanks! :3

	bool foundStairs = false;
	Vector3 wallTestTravel = Vector3.Zero;
	Vector3 wallRemainder = Vector3.Zero;
	Vector3 ceilingPosition = Vector3.Zero;
	float ceilingTravelDistance = 0f;
	KinematicCollision3D wallCollision = null;
	KinematicCollision3D floorCollision = null;

	private bool MoveAndClimbStairs(float delta, bool allowStairSnapping = true)
	{
		Vector3 startPosition = GlobalPosition;
		Vector3 startVelocity = Velocity;

		foundStairs = false;
		wallTestTravel = Vector3.Zero;
		wallRemainder = Vector3.Zero;
		ceilingPosition = Vector3.Zero;
		ceilingTravelDistance = 0f;
		wallCollision = null;
		floorCollision = null;

		// do MoveAndSlide and check if we hit a wall
		MoveAndSlide();
		Vector3 slideVelocity = Velocity;
		Vector3 slidePosition = GlobalPosition;
		bool hitWall = false;
		float floorNormal = Mathf.Cos(FloorMaxAngle);
		int maxSlide = GetSlideCollisionCount() - 1;
		Vector3 accumulatedPosition = startPosition;
		foreach (int slide in GD.Range(maxSlide + 1))
		{
			KinematicCollision3D collision = GetSlideCollision(slide);
			float y = collision.GetNormal().Y;
			if (y < floorNormal && y > -floorNormal)
			{
				hitWall = true;
			}
			accumulatedPosition += collision.GetTravel();
		}
		Vector3 slideSnapOffset = accumulatedPosition - GlobalPosition;

		// if we hit a wall, check for simple stairs; three steps
		if (hitWall && (startVelocity.X != 0f || startVelocity.Z != 0f))
		{
			GlobalPosition = startPosition;
			Velocity = startVelocity;

			// step 1: upwards trace
			float upHeight = StepHeight;
            //chaneged to true
            KinematicCollision3D ceilingCollision = MoveAndCollide(upHeight * Vector3.Up);
            ceilingTravelDistance = StepHeight;
			if (ceilingCollision != null)
				ceilingTravelDistance = Mathf.Abs(ceilingCollision.GetTravel().Y);
			ceilingPosition = GlobalPosition;

			// step 2: "check if there's a wall" trace
			wallTestTravel = Velocity * delta;
			object[] info = MoveAndCollideNTimes(Velocity, delta, 2);
			Velocity = (Vector3)info[0];
			wallRemainder = (Vector3)info[1];
			wallCollision = (KinematicCollision3D)info[2];

			// step 3: downwards trace
			floorCollision = MoveAndCollide(Vector3.Down * (ceilingTravelDistance + (StartedProcessOnFloor ? StepHeight : 0f)));
			if (floorCollision != null)
			{
				if (floorCollision.GetNormal(0).Y > floorNormal)
					foundStairs = true;
			}
		}
		// (this section is more complex than it needs to be, because of MoveAndSlide taking velocity and delta for granted)
		// if we found stairs, climb up them
		if (foundStairs)
		{
			Vector3 vel = Velocity;
			bool StairsCauseFloorSnap = true;
			if (allowStairSnapping && StairsCauseFloorSnap == true)
				vel.Y = 0f;
			Velocity = vel;
			Vector3 oldvel = Velocity;
			Velocity = wallRemainder / delta;
			MoveAndSlide();
			Velocity = oldvel;
		}
		// no stairs, do "normal" non-stairs movement
		else
		{
			GlobalPosition = slidePosition;
			Velocity = slideVelocity;
		}
		return foundStairs;
	}

	private object[] MoveAndCollideNTimes(Vector3 vector, float delta, int slideCount, bool skipRejectIfCeiling = true)
	{
		KinematicCollision3D collision = null;
		Vector3 remainder = vector;
		Vector3 adjustedVector = vector * delta;
		float floorNormal = Mathf.Cos(FloorMaxAngle);
		foreach (int i in GD.Range(slideCount))
		{
			KinematicCollision3D newCollision = MoveAndCollide(adjustedVector);
			if (newCollision != null)
			{
				collision = newCollision;
				remainder = collision.GetRemainder();
				adjustedVector = remainder;
				if (!skipRejectIfCeiling || collision.GetNormal().Y >= -floorNormal)
				{
					adjustedVector = adjustedVector.Slide(collision.GetNormal());
					vector = vector.Slide(collision.GetNormal());
				}
			}
			else 
			{
				remainder = Vector3.Zero;
				break;
			}
		}
		return new object[] { vector, remainder, collision };
	}
	#endregion

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
				grabJoint.NodeB = grabbedObject.GetPath();
            }
        }
	}

	private void DropObject()
	{
		if (grabbedObject != null)
		{
			grabbedObject = null;
			grabJoint.NodeB = grabJoint.GetPath();
		}
	}

	private void RotateObject(InputEvent @event)
	{
		if (grabbedObject != null && @event is InputEventMouseMotion mouseEvent)
		{
			grabStaticBody.RotateY(Mathf.DegToRad(mouseEvent.Relative.X * grabRotationPower));
			grabStaticBody.RotateX(Mathf.DegToRad(mouseEvent.Relative.Y * grabRotationPower));
		}
	}

	private void ProcessGrabbedObject()
	{
		if (grabbedObject != null)
		{
			Vector3 objPosition = grabbedObject.GlobalPosition;
			Vector3 targetPosition = GrabbedObjectPositionMarker.GlobalPosition;

			grabbedObject.LinearVelocity = (targetPosition - objPosition) * grabObjectPullPower;
		}
	}
	#endregion
}
