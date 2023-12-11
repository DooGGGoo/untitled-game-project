using Godot;

public partial class Player : GroundCharacter
{
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.25f;
	[Export] private bool isNoclip = false;
	[Export] private CollisionShape3D StandingCollisionShape, CrouchingCollisionShape;
	[Export] private ShapeCast3D CrouchAboveCheck;
	[Export] private RayCast3D InteractionCheck;
	[Export] private Node3D GrabbedObjectPositionMarker;

	[ExportGroup("Movement")]
	[Export] private float crouchDepth = -0.8f;
	private Vector3 wishDirRaw;

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
	private const float grabRotationPower = 0.5f;
	private bool grabMouseLock;
	private RigidBody3D grabbedObject;

	[Signal] public delegate void AttackPrimaryEventHandler(); 

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayerCamera.MakeCurrent();
		AddToGroup("Player");
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
			if (grabbedObject != null)
			{
				grabMouseLock = true;
				RotateObject(@event);
			}
			else
			{
				EmitSignal(SignalName.AttackPrimary);
			}
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

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		wishDirRaw = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();
		wishDir = (Transform.Basis * new Vector3(wishDirRaw.X, 0, wishDirRaw.Z)).Normalized();

		if (isNoclip == true)
		{
			ProcessMovementNoclip(delta);
		}
		else
		{
			ProcessMovement(delta);
		}

		ProcessCrouchingAndSprint(delta);
		
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

	private void ProcessCrouchingAndSprint(double delta)
	{
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
	} 

	private void ProcessMovementNoclip(double delta)
	{
		Vector3 velocity = Velocity;

		if (wishDirRaw != Vector3.Zero)
			velocity = velocity.Lerp(wishDirRaw * currentSpeed * 2f, 9f * (float)delta);
		else
			velocity = velocity.Lerp(Vector3.Zero, 12f * (float)delta);

		Velocity = velocity;
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
				grabbedObject.GlobalPosition = grabStaticBody.GlobalPosition;
				grabJoint.NodeB = grabbedObject.GetPath();
			}
		}
	}

	private void DropObject()
	{
		if (grabbedObject != null || !IsInstanceValid(grabbedObject))
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
		if (grabbedObject != null && IsInstanceValid(grabbedObject))
		{
			// if object is too far - drop it
			if (grabbedObject.GlobalPosition.DistanceTo(grabStaticBody.GlobalPosition) > 4f)
			{
				DropObject();
			}
		}
		else 
			grabbedObject = null;
	}
	#endregion
}
