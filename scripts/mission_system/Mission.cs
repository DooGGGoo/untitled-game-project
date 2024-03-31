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


    public void Start()
    {
        GetObjectives();

        if (objectives.Count == 0)
        {
            GD.Print("No objectives set!");
            return;
        }

        foreach (Objective objective in objectives)
        {
            objective.ToggleEnabled();
        }
    }

    private void GetObjectives()
    {
        foreach (Objective objective in objectives)
        {
            if (IsInstanceValid(objective))
            {
                DisconnectSignals(objective);
            }
            else
            {
                objective.QueueFree();
            }
        }

        objectives.Clear();


        foreach (NodePath path in ObjectivesPaths)
        {
            Objective objective = Global.Instance.CurrentLevel.GetNodeOrNull<Objective>(path);

            if (!IsInstanceValid(objective))
            {
                GD.Print($"Objective '{path}' is invalid! Node '{objective}'");
                continue;
            }
            else
            {
                objectives.Add(objective);
                ConnectSignals(objective);
            }
        }
    }

    private void ConnectSignals(Objective objective)
    {
        objective.Connect(Objective.SignalName.ObjectiveCompleted, Callable.From(EmitIfCompleted));
    }

    private void DisconnectSignals(Objective objective)
    {
        objective.Disconnect(Objective.SignalName.ObjectiveCompleted, Callable.From(EmitIfCompleted));
    }

    private void EmitIfCompleted()
    {
        if (IsCompleted())
        {
            EmitSignal(SignalName.MissionCompleted);
        }
    }
    
    public bool IsCompleted()
    {
        foreach (Objective objective in objectives)
        {
            if (objective.IsCompleted == false)
            {
                return false;
            }
        }

        return true;
    }

}