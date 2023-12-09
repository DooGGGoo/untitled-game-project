using Godot;

public partial class LevelObjective : Node
{
    [Export] public bool IsCompleted;
    private Global global = Global.Instance();
    private Level currentLevel;

    [Signal] public delegate void ObjectiveCompletedEventHandler();

    public override void _Ready()
    {
        currentLevel = global.CurrentLevel;
    }

    public void CompleteObjective()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;
            EmitSignal(SignalName.ObjectiveCompleted);
        }
    }
}
