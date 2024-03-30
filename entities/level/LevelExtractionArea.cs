using Godot;

[GlobalClass]
public partial class LevelExtractionArea : Area3D
{
	[Export] private float extractionTime = 3f;
	[Export] private int neededObjectives = 1;

	private static int completedObjectives = 0;
	private float timer;
	private bool canExtract = false;
	private bool isCurrentlyExtracting = false;

	[Signal] public delegate void ExtractFromLevelEventHandler();

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
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

	public void OnObjectiveCompleted()
	{
		completedObjectives++;

		if (completedObjectives >= neededObjectives)
		{
			CanExtract();
		}
	}

	public void CanExtract()
	{
		canExtract = true;
		GD.Print("Can extract now!");
	}
}
