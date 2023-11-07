using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public Camera3D PlayerCamera;

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
			mouseMotion.X -= mouseMotionEvent.Relative.Y;
			mouseMotion.X = Mathf.Clamp(mouseMotion.X, -90f, 90f);
			mouseMotion.Y -= mouseMotionEvent.Relative.X; 
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
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
