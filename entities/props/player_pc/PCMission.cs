using Godot;
using System;

public partial class PCMission : Control
{
	[Export] public PCMissionData MissionData;
	[Export] public Label NameLabel;

	public override void _Ready()
	{
		NameLabel.Text = MissionData.Name;
	}

	public void StartMission()
	{
		Global global = Global.Instance();
		GD.Print("Mission started | ", MissionData.Name);
		global.CurrentLevel.ChangeLevel(MissionData.Scene);
	}
}
