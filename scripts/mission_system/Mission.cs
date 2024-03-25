using Godot;
using Godot.Collections;

public partial class Mission : Resource
{
    [Export] public string MissionName;
    [Export(PropertyHint.MultilineText)] public string MissionDescription;
    [Export] public Array<NodePath> Objectives;
    [Export(PropertyHint.File, "*.tscn")] public string LevelScenePath;
    
    [Signal] public delegate void MissionCompletedEventHandler();
    
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

        EmitSignal(SignalName.MissionCompleted);
        return true;
    }
}