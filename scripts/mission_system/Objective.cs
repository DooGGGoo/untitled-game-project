using Godot;

[GlobalClass]
public sealed partial class Objective : Node
{
    [Export] public bool OneShot = true;
    public bool IsCompleted = false;
    
    [Signal] public delegate void ObjectiveCompletedEventHandler();

    public override void _Ready()
    {
        ToggleDisabled();
    }

    public void Complete()
    {
        if (OneShot && !IsCompleted)
        {
            IsCompleted = true;
            EmitSignal(SignalName.ObjectiveCompleted);
            ToggleDisabled();
        }
        else if (!OneShot)
        {
            EmitSignal(SignalName.ObjectiveCompleted);
        }
    }

    public void ToggleEnabled()
    {
        GetParent<Node3D>().Visible = true;
        GetParent<Node3D>().ProcessMode = ProcessModeEnum.Inherit;
    }

    public void ToggleDisabled()
    {
        GetParent<Node3D>().ProcessMode = ProcessModeEnum.Disabled;
        GetParent<Node3D>().Visible = false;
    }
}