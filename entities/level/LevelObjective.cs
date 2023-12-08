using Godot;

public partial class LevelObjective : Node
{
    Global global = Global.Instance();
    private Level currentLevel;

    [Signal] public delegate void ObjectiveCompletedEventHandler();

    public override void _Ready()
    {
        currentLevel = global.CurrentLevel;
    }

    public void CompleteObjective()
    {
        EmitSignal(SignalName.ObjectiveCompleted);
    }
}
