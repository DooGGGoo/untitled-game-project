using Godot;

namespace Missions
{
    [GlobalClass]
    public partial class MissionObjective : Resource
    {
        [Export] public string Title;
        [Export] public string Description;
        public bool IsCompleted;

        public MissionObjective()
        {
            Title = "Objective";
            Description = "Description";
            IsCompleted = false;
        }

        public MissionObjective(string title, string description)
        {
            Title = title;
            Description = description;
            IsCompleted = false;
        }

        [Signal]
        public delegate void MissionObjectiveCompletedEventHandler();

        public virtual void UpdateObjective()
        {
            if (IsCompleted)
            {
                EmitSignal(SignalName.MissionObjectiveCompleted);
            }
            else return;
        }
    }

    [GlobalClass]
    public partial class KillEnemiesObjective : MissionObjective
    {
        [Export] public int EnemiesToKill;
        public int EnemiesKilled;

        public KillEnemiesObjective(string title, string description, int enemiesToKill) : base(title, description)
        {
            EnemiesToKill = enemiesToKill;
            EnemiesKilled = 0;
        }

        public override void UpdateObjective()
        {
            base.UpdateObjective();

            if (EnemiesKilled >= EnemiesToKill)
            {
                IsCompleted = true;
            }
        }
    }
}

