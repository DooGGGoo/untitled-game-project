using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Mission : Resource
{
    [Export] public string MissionName;
    [Export(PropertyHint.MultilineText)] public string MissionDescription;
    [Export] public Array<NodePath> Objectives;
    [Export(PropertyHint.File, "*.tscn")] public string LevelScenePath;
    
    [Signal] public delegate void MissionCompletedEventHandler();

    public void Start()
    {
        if(Objectives.Count == 0) 
        {
            GD.Print("No objectives set!");
            return;
        }

        foreach (NodePath objectivePath in Objectives)
        {
            Objective objective = Global.Instance.CurrentLevel.GetNode<Objective>(objectivePath);

            if (!IsInstanceValid(objective))
            {
                GD.Print($"One of the objectives for mission {MissionName} is invalid!");
                return;
            }

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
        foreach (NodePath objectivePath in Objectives)
        {
            Objective objective = Global.Instance.CurrentLevel.GetNode<Objective>(objectivePath);

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