using System.Collections.Generic;

namespace TTGHotS.Events
{
    class Mission
    {
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
