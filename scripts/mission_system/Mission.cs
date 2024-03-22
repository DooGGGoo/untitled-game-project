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
        [Export(PropertyHint.File, "*.tscn")] public string LevelScenePath;
        [Export] public Array<MissionObjective> Objectives;

        public bool IsComplete => Objectives.All(o => o.IsCompleted);

        public Mission() { }

        public Mission(string title = "Mission", string description = "No description")
        {
            Title = title;
            Description = description;
            Objectives = new() {new("Objective 1", "Lorem ipsum")}; 
        }
    }
}