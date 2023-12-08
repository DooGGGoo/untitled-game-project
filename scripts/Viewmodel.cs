using Godot;
using System;

public partial class Viewmodel : Node3D
{
	[Export] private float swayStrength;
	[Export] private float swaySmoothing;
	private Vector2 swayAmount;

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			swayAmount = Vector2.Zero;
			swayAmount = mouseMotion.Relative;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 rotation = RotationDegrees;
		Vector3 sway = Vector3.Zero;

		float clamp = 3f;

		sway.X = -swayAmount.Y * swayStrength;
		sway.Y = -swayAmount.X * swayStrength;

		if (Mathf.Abs(rotation.X) < 1f || Mathf.Abs(rotation.Y) < 1f)
		{
			rotation = rotation.MoveToward(Vector3.Zero, (float)delta * swaySmoothing);
		}

		rotation = rotation.Lerp(sway, (float)delta * swaySmoothing);

		rotation.X = Mathf.Clamp(rotation.X, -clamp, clamp);
		rotation.Y = Mathf.Clamp(rotation.Y, -clamp, clamp);

		rotation = rotation.Lerp(Vector3.Zero, (float)delta * swaySmoothing * 4f);

		RotationDegrees = rotation;
	}

}
