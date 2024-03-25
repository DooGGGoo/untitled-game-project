using System.Collections.Generic;
using System;
using Godot;

public sealed partial class MissionSystem : Node
{
    private MissionSystem() { }
    private static MissionSystem instance;

    public static MissionSystem Instance
    {
        get => instance ??= new MissionSystem();
        private set => instance = value;
    }

    public List<Mission> AvailableMissions = [];
    public Mission ActiveMission;

    public override void _EnterTree()
    {
        LoadMissionsFromDir();
    }

    public Mission StartRandomMission()
    {
        if (AvailableMissions.Count == 0) return null;

        int randomIndex = GD.RandRange(0, AvailableMissions.Count);
        Mission randomMission = AvailableMissions[randomIndex];

        return ActiveMission != null ? null : StartMission(randomMission);
    }

    public Mission StartMission(Mission mission)
    {
        if (mission == null) return mission;

        if (ActiveMission != null)
        {
            GD.PrintErr("Attempted to start a new mission when one already is active!");
            return mission;
        }

        AvailableMissions.Remove(mission);
        ActiveMission = mission;

        ActiveMission.MissionCompleted += EndMission;

        return ActiveMission;
    }

    public void EndMission()
    {
        if (ActiveMission == null) return;

        AvailableMissions.Add(ActiveMission);
        ActiveMission = null;
    }

    private void LoadMissionsFromDir(string dirPath = "res://missions/")
    {
        if (DirAccess.Open(dirPath) == null)
        {
            GD.PrintErr("Mission directory does not exist!");
            return;
        }

        DirAccess dir = DirAccess.Open(dirPath);

        foreach (string fileName in dir.GetFiles())
        {
            if (fileName != "" )
            {
                string filename = fileName.Replace(".import", ""); // This fixes filenames changing when exporting the project, see https://forum.godotengine.org/t/cannot-traverse-asset-directory-in-android/20496/2
                if (filename.EndsWith(".tres"))
                {
                    try
                    {
                        Mission missionToLoad = ResourceLoader.Load<Mission>(filename);
                        AvailableMissions.Add(missionToLoad);
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("Failed to load mission: " + e.Message);
                    }
                }
            }
        }

        GD.Print("Loaded " + AvailableMissions.Count + " missions.");
    }
}