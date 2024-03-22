using Godot;
using Missions;

public partial class MissionUIDisplay : Control
{
    [Export] private Label missionTitleLabel;
    [Export] private Label missionDescriptionLabel;
    [Export] private Button acceptButton;

    public override void _Ready()
    {
        //SetMission(new Mission("Test Mission", "Test Description"));
    }

    public void SetMission(Mission mission)
    {
        missionTitleLabel.Text = mission.Title;
        missionDescriptionLabel.Text = mission.Description;

        acceptButton.Pressed += () =>
        {
            GD.Print($"Accepted mission {mission.Title}");
            PackedScene level = ResourceLoader.Load<PackedScene>(mission.LevelScenePath);
            GetTree().ChangeSceneToPacked(level);
        };
    }
}
