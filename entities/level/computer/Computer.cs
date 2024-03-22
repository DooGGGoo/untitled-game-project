using Godot;
using Godot.Collections;
using Missions;

public partial class Computer : Node3D, IInteractable
{
	[Export] private Camera3D PCCamera;
	[Export] private Array<Mission> possibleMissions = new();
	[Export] private Array<Mission> missions = new();
	[Export] private VBoxContainer missionsContainer;
	[Export] private Button testAddMission;
	[Export] private PackedScene UImissionScene;

	private Player player;
	private Camera3D playerCamera;
	private bool isActive = false;

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("esc"))
		{
			if (PCCamera.Current)
			{
				PCCamera.Current = false;
				player.PlayerView.LockCameraRotation = false;
				player.ProcessMode = ProcessModeEnum.Inherit;
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}

	public void Interact(CharacterBody3D interactor)
	{
		if (interactor is not Player player) return;

		this.player = player;

		playerCamera = player.PlayerView.PlayerCamera;
		player.PlayerView.LockCameraRotation = true;
		player.ProcessMode = ProcessModeEnum.Disabled;

		Camera3D spinCam = (Camera3D)playerCamera.Duplicate();
		spinCam.Transform = playerCamera.GlobalTransform;
		GetParent().AddChild(spinCam);

		Tween tween = CreateTween();

		tween.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(spinCam, "transform", PCCamera.GlobalTransform, 0.6f);
		tween.Parallel().TweenProperty(spinCam, "fov", PCCamera.Fov, 0.6f);
		tween.TweenCallback(Callable.From(CurrentCameraPC));

		tween.Play();
		spinCam.MakeCurrent();

		tween.Finished += () => spinCam.QueueFree();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		isActive = true;
	}

	private void CurrentCameraPC()
	{
		PCCamera.MakeCurrent();
	}
	
	public Mission GetRandomMission()
	{
		if (possibleMissions.Count != 0)
			return possibleMissions.PickRandom();
		else return null;
	}

	public Mission CreateDummyMission()
	{
		Mission mission = new($"{GD.Randi()}", $"{GD.Randi()}");
		missions.Add(mission);
		return mission;
	}

	public void CreateTestMission()
	{
		Mission newMission = CreateDummyMission();
		MissionUIDisplay uiDisplay = UImissionScene.Instantiate<MissionUIDisplay>();
		uiDisplay.SetMission(newMission);
		missionsContainer.AddChild(uiDisplay);
	}

	public void CreateRandomMission()
	{
		Mission newMission = GetRandomMission();
		MissionUIDisplay uiDisplay = UImissionScene.Instantiate<MissionUIDisplay>();
		uiDisplay.SetMission(newMission);
		missionsContainer.AddChild(uiDisplay);
	}
}
