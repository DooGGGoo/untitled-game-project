using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Mission : Resource
{
    [Export] public string MissionName;
    [Export(PropertyHint.MultilineText)] public string MissionDescription;
    [Export] public Array<NodePath> ObjectivesPaths;
    [Export(PropertyHint.File, "*.tscn")] public string LevelScenePath;
    private readonly List<Objective> objectives = [];
    
    [Signal] public delegate void MissionCompletedEventHandler();

    private void GetObjectivesFromPaths()
    {
        foreach (NodePath objectivePath in ObjectivesPaths)
        {
            Objective objective = Global.Instance.CurrentLevel.CurrentPlayer.GetTree().CurrentScene.GetNode(objectivePath) as Objective;

    

            GD.Print(objective);

            if (objective == null)
            {
                GD.Print($"One of the objectives for mission {MissionName} is invalid!");
                return;
            }

            objectives.Add(objective);

            objective.ObjectiveCompleted += () =>
            {
                if (IsCompleted())
                {
                    EmitSignal(SignalName.MissionCompleted);
                }
            };
        }
    }

    public void Start()
    {
        GetObjectivesFromPaths();

        if (objectives.Count == 0) 
        {
            GD.Print("No objectives set!");
            return;
        }

        foreach (Objective objective in objectives)
        {
            if (!IsInstanceValid(objective))
            {
                GD.Print($"One of the objectives for mission {MissionName} is invalid!");
                return;
            }

            objective.ToggleEnabled();

            objective.ObjectiveCompleted += () => 
            {
                if (IsCompleted())
                {
                    EmitSignal(SignalName.MissionCompleted);
                }
            };
        }
    }
    
    public bool IsCompleted()
    {
        foreach (Objective objective in objectives)
        {
            if (!IsInstanceValid(objective))
            {
                GD.Print($"One of the objectives for mission {MissionName} is invalid!");
                return false;
            }

            if (objective.IsCompleted == false)
            {
                return false;
            }
        }

        return true;
    }
}