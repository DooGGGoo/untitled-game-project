using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public Camera3D PlayerCamera;
	[Export] public float Sensitivity = 0.5f; 

	private Vector2 mouseMotion;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	public float gravity = 9.8f;


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
        base._Input(@event);
    }

    public override void _PhysicsProcess(double delta)
	{
		PlayerCamera.RotationDegrees = new Vector3(mouseMotion.X, mouseMotion.Y, PlayerCamera.RotationDegrees.Z);
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 wishDir = (PlayerCamera.GlobalTransform.Basis.X * inputDir.X + -PlayerCamera.GlobalTransform.Basis.Z * -inputDir.Y).Normalized();
		Vector3 direction = (Transform.Basis * new Vector3(wishDir.X, 0, wishDir.Z)).Normalized();

		if (direction != Vector3.Zero)
		{
		//															 \/ TODO: Acceleration and friction values
			velocity.X = Mathf.Lerp(velocity.X, direction.X * Speed, 7f * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * Speed, 7f * (float)delta);
		}
		else
		{
			velocity.X = Mathf.Lerp(velocity.X, 0f, 9f * (float)delta);
			velocity.Z = Mathf.Lerp(velocity.Z, 0f, 9f * (float)delta);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
