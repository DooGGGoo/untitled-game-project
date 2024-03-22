using System.Linq;
using Godot;
using Godot.Collections;

namespace Missions
{
    public partial class LevelObjectivePoint : Node3D
    {
        public enum PointType 
        {
            General,
            Enemy,
            PickUp,
            Location,
        }

        [Export] public Array PossibleSpawnTypes;
        [Export] public PointType ObjectivePointType = PointType.General;

        public override void _Ready()
        {
        }
    }
}