using System;
using Newtonsoft.Json.Linq;

namespace TTGHotS.Events
{
    class Event
    {
        public string name;
        public int cost; // The current cost
        public int basecost; // The cost per tier, if applicable
        public int bank; // The currently donated credits contributing to the next activation
        public int tier;
        public string donationType; // "onetime", "dynamic", "group" and "repeatable"
        public string mission; // which mission this can be activated on, or "all" if it's anywhere
        public string category; // "generic", "mission", "units", "soa", "admin"
        public bool stackable;
        public bool allowedInNoBuild;
        public string alignment; // "positive", "negative" or "neutral"
        public string description;
        public string descriptionAnsi; // the description with coloring!

        public Event()
        {

        }

        public Event(JObject data)
        {
            name = data["name"].ToString();
            cost = int.Parse(data["cost"].ToString());
            donationType = data["donationType"].ToString();
            mission = data["mission"].ToString();
            category = data["category"].ToString();  // cost > 99999 ? "admin" : (mission == Mission.ALL ? "generic" : "mission");
            bank = int.Parse(data["bank"].ToString());
            tier = int.Parse(data["tier"].ToString());
            stackable = bool.Parse(data["stackable"].ToString());
            allowedInNoBuild = bool.Parse(data["allowedInNoBuild"].ToString());
            alignment = data["alignment"].ToString(); ;
            description = data["description"].ToString();

            descriptionAnsi = data.ContainsKey("descriptionAnsi") && !string.IsNullOrWhiteSpace(data["descriptionAnsi"].ToString()) ? data["descriptionAnsi"].ToString() : description;

            if (donationType == "dynamic")
            {
                basecost = int.Parse(data["basecost"].ToString());
                Console.WriteLine("Found basecost for " + name + " at " + basecost);
            }
            else
            {
                basecost = 0;
            }
        }

        public int CheckCost()
        {
            return bank / cost;
        }

        public void CallEvent(double multiplier)
        {
            bank -= GetMultiplierCost(multiplier);
            tier++;
        }

        public void CalculateNewCost()
        {
            if (donationType == "dynamic")
            {
                Console.WriteLine("BC: " + basecost + " Tier: " + tier + " Cost: " + cost);
                cost = basecost * (tier + 1);
            }
        }

        public int GetCostToNextActivation(double multiplier)
        {
            return GetMultiplierCost(multiplier) - GetBank();
        }

        public int GetBank()
        {
            return bank;
        }

        public bool IsStackable()
        {
            return stackable;
        }

        public void AddToBank(int amountToAdd)
        {
            bank += amountToAdd;
        }

        public void SetBank(int newBank)
        {
            bank = newBank;
        }

        public int GetMultiplierCost(double multiplier)
        {
            return (int)Math.Ceiling(cost * multiplier);
        }

        public void SetCost(int newCost)
        {
            cost = newCost;
        }

        public void SetCostWithMultiplier(int newCost, double multiplier)
        {
            cost = (int)Math.Round(newCost / multiplier);
        }
    }
}
