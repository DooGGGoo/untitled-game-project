using Godot;

public partial class Player : GroundCharacter
{
	[ExportGroup("Movement")]
	[Export] private bool isNoclip = false;
	[Export] private CollisionShape3D StandingCollisionShape, CrouchingCollisionShape;
	[Export] private ShapeCast3D CrouchAboveCheck;
	[Export] private float crouchDepth = -0.8f;
	private Vector3 wishDirRaw;

	[ExportGroup("Camera")]
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.25f;
	[Export] public float CameraRotationLimit = 0.25f;
	[Export] private float cameraRollAngle = .5f;
	[Export] private float cameraRollSpeed = 3f;
	[Export] public bool enableHeadbob = true;
	[Export] private float headbobTimer = 10f;  // Speed
	[Export] private float headbobScale = 0.1f; // Magnitude
	[Export] private float cameraShakeReductionRate = 1f;
	[Export] private FastNoiseLite noise = new();
	[Export] private float noiseSpeed = 50f;
	[Export] private Vector3 maxShakeRotation;
	private float cameraShake;
	private float time;
	private Vector3 cameraTargetRotation, shakeInitialRotation, viewmodelInitialPosition, oldPosition;
	private Vector2 mouseInput;

	[ExportSubgroup("Viewmodel")]
	[Export] private Node3D viewmodel;
	[Export] private float bobCycle;
	[Export] private float bobUp;
	[Export] private float bobAmount;
	private Vector3 bobTimes, bobOffsets;

	[ExportGroup("Object interactions")]
	[Export] private RayCast3D InteractionCheck;
	[Export] private Node3D GrabbedObjectPositionMarker;
	[Export] private Generic6DofJoint3D grabJoint;
	[Export] private StaticBody3D grabStaticBody;
	private const float grabRotationPower = 0.5f;
	private bool grabMouseLock;
	private RigidBody3D grabbedObject;

	[ExportGroup("Sounds")]
	private float footstepsTimer;
	private bool footstepCanPlay;

	[Signal] public delegate void AttackPrimaryEventHandler(); 

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayerCamera.MakeCurrent();

		shakeInitialRotation = PlayerCamera.RotationDegrees;
		viewmodelInitialPosition = viewmodel.Position;

		// We disabling that to fix "jumping" values at low framerate for example in Lerp function
		Input.UseAccumulatedInput = false;

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

			mouseInput = Vector2.Zero;
			mouseInput = -mouseMotionEvent.Relative;
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

		if (@event.IsAction("debug_1"))
		{
			AddCameraShake(0.5f);
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
		footstepsTimer += (float)delta * Velocity.Length() * (IsOnFloor() ? 1 : 0f);

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
		CalculateFootsteps();
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

			cameraTargetRotation.Y += offset.Y;
		}

		ProcessCameraShake(delta);

		// Apply all rotation changes
		cameraTargetRotation.X = Mathf.Clamp(cameraTargetRotation.X, -89.9f, 89.9f);
		PlayerCamera.RotationDegrees = new Vector3(cameraTargetRotation.X, cameraTargetRotation.Y, cameraZRotation);
	}

	private void ProcessCameraShake(double delta)
	{
		cameraShake = Mathf.Max(cameraShake - (float)delta * cameraShakeReductionRate, 0f);

		cameraTargetRotation.X += shakeInitialRotation.X + maxShakeRotation.X * GetCameraShakeIntensity() * GetNoiseFromSeed(0);
		cameraTargetRotation.Y += shakeInitialRotation.Y + maxShakeRotation.Y * GetCameraShakeIntensity() * GetNoiseFromSeed(1);
		cameraTargetRotation.Z += shakeInitialRotation.Z + maxShakeRotation.Z * GetCameraShakeIntensity() * GetNoiseFromSeed(2);
	}

	public void AddCameraShake(float amount)
	{
		cameraShake = Mathf.Clamp(cameraShake + amount, 0f, 1f);
	}

	private float GetCameraShakeIntensity()
	{
		return cameraShake * cameraShake;
	}

	private float GetNoiseFromSeed(int seed)
	{
		noise.Seed = seed;
		return noise.GetNoise1D(time * noiseSpeed);
	}

	public void ViewPunch(Vector3 angle, bool? useSmoothing = false)
	{
		if (useSmoothing == true)
		{
			cameraTargetRotation = cameraTargetRotation.Slerp(cameraTargetRotation + angle, 0.25f);
		}
		else
		{
			cameraTargetRotation += angle;
		}
	}

	// TODO
	#region Viewmodel
	private void ProcessViewmodel()
	{
		Vector3 offset = new()
        {
            //Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
            //X = Mathf.Cos(time * headbobTimer / 2f) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
			Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
			X = Mathf.Sin((time * headbobTimer +  Mathf.Pi * 3f) / -2f) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
        };

        viewmodel.Position += offset;
		viewmodel.Position = viewmodel.Position.Lerp(viewmodelInitialPosition, 0.125f);

		Vector3 viewmodelRotation = viewmodel.RotationDegrees;

		viewmodelRotation.X = Mathf.Lerp(viewmodel.Rotation.X, mouseInput.Y * .9f, 0.125f);
		viewmodelRotation.Y = Mathf.Lerp(viewmodel.Rotation.Y, mouseInput.X * .9f, 0.125f);

		viewmodel.RotationDegrees += viewmodelRotation;

		viewmodel.RotationDegrees = viewmodel.RotationDegrees.Clamp(new Vector3(-6f, -6f, -6f), new Vector3(6f, 6f, 6f));
		viewmodel.RotationDegrees = viewmodel.RotationDegrees.Lerp(Vector3.Zero, 0.125f);
	}

	#endregion
	
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

	#region Sounds

	[Signal]
	public delegate void PlayFootstepSoundEventHandler();

	public virtual void CalculateFootsteps()
	{
		const float freq = 5.85f;
		const float amp = 0.08f;
		float pos;
		float lowPos = amp - 0.05f;

		pos = Mathf.Sin(footstepsTimer * freq) * amp;
		if (pos > -lowPos)
		{
			footstepCanPlay = true;
		}

		if (pos < -lowPos && footstepCanPlay)
		{
			footstepCanPlay = false;
			
			
			if (Mathf.Abs(Velocity.X) > WalkSpeed || Mathf.Abs(Velocity.Z) > WalkSpeed)
			{
				EmitSignal(SignalName.PlayFootstepSound);
			}
			else
			{
				EmitSignal(SignalName.PlayFootstepSound);
			}
		}
	}
	#endregion
}
