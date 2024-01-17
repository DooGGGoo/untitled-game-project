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
	[Export] public View PlayerView;

	[ExportGroup("Object interactions")]
	[Export] private RayCast3D InteractionCheck;
	[Export] private Node3D GrabbedObjectPositionMarker;
	[Export] private Generic6DofJoint3D grabJoint;
	[Export] private StaticBody3D grabStaticBody;
	private const float grabRotationPower = 0.5f;
	public bool GrabMouseLock;
	private RigidBody3D grabbedObject;

	[ExportGroup("Sounds")]
	private float footstepsTimer;
	private bool footstepCanPlay;

	private Vector3 oldPosition;
	private float time;

	[Signal] public delegate void AttackPrimaryEventHandler();
	[Signal] public delegate void AddPlayerWeaponEventHandler(PackedScene weaponScene);

	public override void _Ready()
	{
		AddToGroup("Player");
	}

	public override void _Input(InputEvent @event)
	{
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
				GrabMouseLock = true;
				RotateObject(@event);
			}
			else
			{
				EmitSignal(SignalName.AttackPrimary);
			}
		}

		if (Input.IsActionJustReleased("left_click"))
		{
			GrabMouseLock = false;
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

		if (Input.IsActionJustPressed("debug_1"))
		{
			Global.Instance().CurrentLevel.SpawnExplosion(GlobalPosition);
		}
	}

	bool StartedProcessOnFloor = false;
	public override void _PhysicsProcess(double delta)
	{
		time += (float)delta;
		footstepsTimer += (float)delta * Velocity.Length() * (IsOnFloor() ? 1 : 0f);

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		wishDirRaw = (PlayerView.GlobalTransform.Basis.X * inputDir.X + -PlayerView.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();
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

		oldPosition = PlayerView.GlobalPosition;

		ProcessGrabbedObject();
		PushRigidBodies(delta);
		StartedProcessOnFloor = IsOnFloor();
		MoveAndClimbStairs((float)delta, false);
		SmoothCameraOnStairs(delta);
		CalculateFootsteps();
	}

	private void SmoothCameraOnStairs(double delta)
	{
		// Smooth camera when moving up stairs
		if (IsOnFloor() && PlayerView.GlobalPosition.Y - oldPosition.Y > 0)
		{
			float stepTime = (float)delta;

			oldPosition.Y += stepTime * 0.5f;

			if (oldPosition.Y > PlayerView.GlobalPosition.Y)
				oldPosition.Y = PlayerView.GlobalPosition.Y;

			if (PlayerView.GlobalPosition.Y - oldPosition.Y > 0.14f)
				oldPosition.Y = PlayerView.GlobalPosition.Y - 0.14f;

			Vector3 pos = PlayerView.GlobalPosition;
			pos.Y += oldPosition.Y - PlayerView.GlobalPosition.Y;
			PlayerView.GlobalPosition = pos;
		}
		else
			oldPosition.Y = PlayerView.GlobalPosition.Y;
	}

	public void AddWeapon(PackedScene weaponScene)
	{
		EmitSignal(SignalName.AddPlayerWeapon, weaponScene);
	}

	#region Movement

	private void ProcessCrouchingAndSprint(double delta)
	{
		if (Input.IsActionPressed("crouch"))
		{
			currentSpeed = CrouchSpeed;
			PlayerView.Position = new Vector3(PlayerView.Position.X, Mathf.Lerp(PlayerView.Position.Y, 1.5f + crouchDepth, 7f * (float)delta), PlayerView.Position.Z);
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

			PlayerView.Position = new Vector3(PlayerView.Position.X, Mathf.Lerp(PlayerView.Position.Y, 1.5f, 7f * (float)delta), PlayerView.Position.Z);
			
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

	[Signal] public delegate void PlayFootstepSoundEventHandler();

	public virtual void CalculateFootsteps()
	{
		const float freq = 3.85f;
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

			EmitSignal(SignalName.PlayFootstepSound);
		}
	}
	#endregion
}
