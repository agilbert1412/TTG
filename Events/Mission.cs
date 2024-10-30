using System.Collections.Generic;

namespace TTGHotS.Events
{
    class Mission
    {
        public const string ALL = "all";
        public static readonly List<string> NO_BUILD_MISSIONS = new List<string> { "For Aiur!", "The Infinite Cycle", "Templar's Return" };

        public Goal missionGoal = new Goal();
        public List<Event> missionEvents = new List<Event>();
        public string name;
        public bool hasMetGoal;

        public Mission()
        {

        }

        public Mission(string _name)
        {
            name = _name;
        }
    }
}
