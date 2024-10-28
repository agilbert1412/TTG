using System;
using Newtonsoft.Json.Linq;

namespace TTGHotS.Events
{
    class Event
    {
        public string name;
        public int cost;
        public int bank;
        public int tier;
        public string donationType;
        public string mission;
        public int basecost;
        public bool stackable;
        public string description;

        public Event()
        {

        }

        public Event(JObject data)
        {
            name = data["name"].ToString();
            cost = int.Parse(data["cost"].ToString());
            donationType = data["donationType"].ToString();
            mission = data["mission"].ToString();
            bank = int.Parse(data["bank"].ToString());
            tier = int.Parse(data["tier"].ToString());
            stackable = bool.Parse(data["stackable"].ToString());
            description = data["description"].ToString();

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
