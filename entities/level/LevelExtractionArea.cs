using Godot;
using System;

[GlobalClass]
public partial class LevelExtractionArea : Area3D
{
	[Export] private float extractionTime = 3f;
	private bool canExtract = false;
	private bool isCurrentlyExtracting = false;
	private float timer;

	[Signal] public delegate void ExtractFromLevelEventHandler();

    public override void _Ready()
    {
        BodyEntered += (Node3D body) => OnBodyEntered(body);
		BodyExited += (Node3D body) => OnBodyExited(body);
    }

	public override void _PhysicsProcess(double delta)
	{
		if (canExtract && isCurrentlyExtracting)
		{
			timer += (float)delta;

			if (timer >= extractionTime)
			{
				isCurrentlyExtracting = false;
				timer = 0f;
				EmitSignal(SignalName.ExtractFromLevel);
			}
		}
	}

	public void OnBodyEntered(Node3D body)
	{
        if (body is Player && !isCurrentlyExtracting)
		{
			isCurrentlyExtracting = true;
			timer = 0f;
		}
	}

	public void OnBodyExited(Node3D body)
	{
        if (body is Player)
		{
			isCurrentlyExtracting = false;
			timer = 0f;
		}
	}

	public void CanExtract()
	{
		canExtract = true;
	}
}
