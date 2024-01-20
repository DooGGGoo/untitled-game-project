using Godot;
using System;
using System.Collections.Generic;

public partial class Airstrike : Node3D
{
	[Export] public float TimeBetweenShots = 0.15f;
	[Export] public float PositionRandomness = 0.1f;
	[Export] public float Spacing = 0.25f;
	[Export] public int HitsCount = 6;

	public List<Node3D> HitPoints = new();

	[Signal] public delegate void AirstrikeCalledEventHandler();
	[Signal] public delegate void AirstrikeFinishedEventHandler();

	public void CallAirstrike()
	{
		RotateY(Mathf.DegToRad(GD.RandRange(-360, 360)));

		for (int i = 0; i < HitsCount; i++)
		{
            Node3D hitPoint = new()
            {
                Position = new Vector3(Spacing * i, 0, 0) + new Vector3(
					(float)GD.RandRange(-PositionRandomness, PositionRandomness), 
					0, 
					(float)GD.RandRange(-PositionRandomness, PositionRandomness)
				)
            };


			AddChild(hitPoint);
			HitPoints.Add(hitPoint);
        }

		EmitSignal(SignalName.AirstrikeCalled);
	}

	public void SpawnExplosions()
	{
		foreach (Node3D hitPoint in HitPoints)
		{
			SpawnExplosion(hitPoint.GlobalTransform.Origin);
		}

		EmitSignal(SignalName.AirstrikeFinished);
	}

	private async void SpawnExplosion(Vector3 position)
	{
		await ToSignal(GetTree().CreateTimer(TimeBetweenShots), "timeout");
		Global.Instance().CurrentLevel.SpawnExplosion(position);
	}

}
