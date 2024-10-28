using Newtonsoft.Json.Linq;
using System;

namespace TTGHotS.Events
{
    class Goal
    {
        public int cost;
        public int currentBank;
        public int displayBank;
        public int repeatCount;
        public int scalingValue;
        public string mission;
        public string planet;
        public string timeframe;
        public string type;

        public Goal()
        {

        }

        public Goal(JObject data)
        {
            cost = int.Parse(data["cost"].ToString());
            currentBank = int.Parse(data["currentBank"].ToString());
            displayBank = currentBank;
            mission = data["mission"].ToString();
            planet = data["planet"].ToString();
            timeframe = data["timeframe"].ToString();
            type = data["type"].ToString();
            if (type == "repeatable")
            {
                repeatCount = 0;
            }
            else
            {
                repeatCount = -1;
            }
            scalingValue = int.Parse(data["scalingValue"].ToString());
        }

        public void AddCredits(int creditCount)
        {
            currentBank += creditCount;
            if (type == "onetime")
            {
                displayBank += creditCount;
            }
            if (type == "repeatable")
            {
                displayBank = currentBank;
            }
            if (type == "campaign")
            {
                displayBank += creditCount;
            }
        }

        //I need to make sure that planet goals trigger multiple times if needed
        //Also, they need to have initial cost + additional cost and have it output that.
    }
}
