using Godot;

public sealed partial class Objective : Node
{
    [Export] public bool OneShot = true;
    public bool IsCompleted = false;
    
    [Signal] public delegate void ObjectiveCompletedEventHandler();

    public override void _EnterTree()
    {
        GetParent<Node3D>().Visible = false;
    }

    public void Complete()
    {
        if (OneShot && !IsCompleted)
        {
            IsCompleted = true;
            EmitSignal(SignalName.ObjectiveCompleted);
        }
        else if (!OneShot)
        {
            EmitSignal(SignalName.ObjectiveCompleted);
        }
    }
}