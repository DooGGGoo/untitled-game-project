using System.Linq;
using Godot;
using Godot.Collections;

namespace Missions
{
    [GlobalClass]
    public partial class Mission : Resource
    {
        [Export] public string Title;
        [Export] public string Description;
        [Export] public PackedScene Level;
        [Export] public Array<MissionObjective> Objectives;

        public bool IsComplete => Objectives.All(o => o.IsCompleted);

        public Mission(string title = "Mission", string description = "No description")
        {
            Title = title;
            Description = description;
            Level = ResourceLoader.Load<PackedScene>("res://maps/map_test_2.tscn");
            Objectives = new() {new("Objective 1", "Lorem ipsum")}; 
        }
    }
}