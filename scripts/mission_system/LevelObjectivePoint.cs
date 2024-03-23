using System.Linq;
using Godot;
using Godot.Collections;

namespace Missions
{
    [GlobalClass]
    public partial class LevelObjectivePoint : Node3D
    {
        public enum PointType 
        {
            General,
            Enemy,
            PickUp,
            Location,
        }

        [Export(PropertyHint.Range, "0,100")] public int SpawnChance = 100;
        [Export] public Array PossibleSpawnTypes;
        [Export] public PointType ObjectivePointType = PointType.General;

        public void SpawnObjective()
        {
            Variant objectToSpawn = PossibleSpawnTypes.PickRandom();

            GD.Print($"Trying to spawn objective of type {objectToSpawn} with chance {SpawnChance} at {GlobalPosition}, spawner node {GetPath()}");

            RandomNumberGenerator rng = new();

            if (SpawnChance > 0 && rng.RandiRange(0, 100) >= SpawnChance)
            {
                // TODO: Spawn object of types

                StandardMaterial3D MaterialOverride = new()
                {
                    AlbedoColor = new Color(0, 0, 1),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
                };

                MeshInstance3D debugMesh = new()
                {
                    Mesh = new SphereMesh()
                    {
                        Rings = 4,
                        RadialSegments = 8,
                        Radius = 0.1f,
                        Height = 0.1f,
                    },
                    MaterialOverride = MaterialOverride,
                    GlobalPosition = GlobalPosition
                };

                AddChild(debugMesh);
            }

        }
    }
}