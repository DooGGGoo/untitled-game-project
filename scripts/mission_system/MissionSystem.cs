using System.Collections.Generic;
using System;
using Godot;
using System.Linq;

public sealed partial class MissionSystem : Node
{
    private MissionSystem() { }
    private static MissionSystem instance;

    public static MissionSystem Instance
    {
        get => instance ??= new MissionSystem();
        private set => instance = value;
    }

    public readonly List<Mission> AvailableMissions = [];
    public Mission ActiveMission;

    public override void _Ready()
    {
        //LoadMissionsFromDir();
    }

    public Mission StartRandomMission()
    {
        if (AvailableMissions.Count == 0)
        {
            LoadMissionsFromDir();
            if (AvailableMissions.Count == 0)
            {
                GD.Print("No missions available");
                return null; 
            }
        }

        int randomIndex = GD.RandRange(0, AvailableMissions.Count - 1);
        Mission randomMission = AvailableMissions[randomIndex];

        return StartMission(randomMission);
    }


    public Mission StartMission(Mission mission) 
    {
        if (mission == null) return mission;

        if (ActiveMission != null) 
        {
            GD.PrintErr("Attempted to start a new mission when one already is active!");
            return null;
        }

        ActiveMission = mission;
        ActiveMission.MissionCompleted += EndMission;
        
        AvailableMissions.Remove(mission);

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

        int loadSuccess = 0;
        int loadFailed = 0;

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
                        Mission missionToLoad = GD.Load<Mission>(dirPath + filename);
                        AvailableMissions.Add(missionToLoad);
                        GD.Print($"Loaded {missionToLoad.MissionName} mission.");
                        loadSuccess++;
                        missionToLoad.Changed += () => GD.Print($"{missionToLoad} changing");
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("Failed to load mission: " + e.Message);
                        loadFailed++;
                    }
                }
            }
        }



        GD.Print($"Loaded {AvailableMissions.Count} missions ({loadSuccess} successfully, {loadFailed} failed).");
    }
}