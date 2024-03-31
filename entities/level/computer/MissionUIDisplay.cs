using Godot;

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
        missionTitleLabel.Text = mission.MissionName;
        missionDescriptionLabel.Text = mission.MissionDescription;

        acceptButton.Pressed += () =>
        {
            GD.Print($"Accepted mission {mission.MissionName}");
            PackedScene level = ResourceLoader.Load<PackedScene>(mission.LevelScenePath);
            Global.Instance.CurrentLevel.ChangeLevel(level);
            //mission.Start();
        };
    }
}
