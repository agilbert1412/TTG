using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace TTGHotS
{
    internal class XmlHandler
    {
        public const string fileWithBankPath = @"UserSpecificFiles\bank.txt";

        public readonly string Bankpath;
        public readonly string BattleNetBank; // This is the one to use
        public readonly string LocalBank;
        public readonly string SpearOfAdunBank;
        public static readonly string XML_EVENT = XmlValuePath("Event", "Name"); // "/Bank/Section[@name='Event']/Key[@name='Name']/Value";
        public static readonly string XML_LOCK = XmlValuePath("Event", "Lock"); // "/Bank/Section[@name='Event']/Key[@name='Lock']/Value";
        public static readonly string XML_VALUE = XmlValuePath("Event", "Value"); // "/Bank/Section[@name='Event']/Key[@name='Value']/Value";
        public static readonly string XML_ARGS = XmlValuePath("Event", "Args"); // "/Bank/Section[@name='Event']/Key[@name='Args']/Value";
        public static readonly string XML_USERNAME = XmlValuePath("Event", "Username"); // "/Bank/Section[@name='Event']/Key[@name='Username']/Value";
        public static readonly string XML_PLANET = XmlValuePath("CurrentMission", "Planet"); // "/Bank/Section[@name='CurrentMission']/Key[@name='Planet']/Value";
        public static readonly string XML_MISSION = XmlValuePath("CurrentMission", "Name"); // "/Bank/Section[@name='CurrentMission']/Key[@name='Name']/Value";

        public static readonly string XML_SOA1 = XmlValuePath("SpearOfAdun", "Ability1"); // "/Bank/Section[@name='SpearOfAdun']/Key[@name='Ability1']/Value";
        public static readonly string XML_SOA2 = XmlValuePath("SpearOfAdun", "Ability2"); // "/Bank/Section[@name='SpearOfAdun']/Key[@name='Ability2']/Value";
        public static readonly string XML_SOA3 = XmlValuePath("SpearOfAdun", "Ability3"); // "/Bank/Section[@name='SpearOfAdun']/Key[@name='Ability3']/Value";
        public static readonly string XML_SOA4 = XmlValuePath("SpearOfAdun", "Ability4"); // "/Bank/Section[@name='SpearOfAdun']/Key[@name='Ability4']/Value";
        public static string[] XML_SOA = { XML_SOA1, XML_SOA2, XML_SOA3, XML_SOA4 };

        private static readonly Dictionary<string, string> _nodeTypes = new Dictionary<string, string>()
        {
            { XML_EVENT, "string" },
            { XML_VALUE, "int" },
            { XML_LOCK, "int" },
            { XML_USERNAME, "string" },
            { XML_PLANET, "string" },
            { XML_MISSION, "text" },
            { XML_ARGS, "string" },
            { XML_SOA1, "string" },
            { XML_SOA2, "string" },
            { XML_SOA3, "string" },
            { XML_SOA4, "string" },
        };


        private static string XmlValuePath(string sectionName, string keyName)
        {
            return $"/Bank/Section[@name='{sectionName}']/Key[@name='{keyName}']/Value";
        }

        public XmlHandler()
        {
            if (!File.Exists(fileWithBankPath))
            {
                throw new FileNotFoundException(@$"Could not find file {fileWithBankPath}. This file needs to exist, and contain the path to your SC2 bank files. Example: 'C:\Users\yourname\Documents\StarCraft II\Accounts\accountnumber\identifier\Banks'");
            }

            Bankpath = File.ReadAllText(fileWithBankPath);
            BattleNetBank = @$"{Bankpath}\EventsBank.SC2Bank"; // This is the one to use
            LocalBank = @$"{Bankpath}\EventsBank.SC2Bank";
            SpearOfAdunBank = @$"{Bankpath}\TTGSoABank.SC2Bank";
        }

        public bool IsBankFileLocked()
        {
            return ReadXML(XML_LOCK) != "-1";
        }

        public void LockBankFile()
        {
            WriteXML(XML_LOCK, "1");
        }

        public string GetCurrentMission(Format format)
        {
            var currentMission = ReadXML(XML_MISSION);
            return FormatString(currentMission, format);
        }

        public string GetCurrentPlanet(Format format)
        {
            var currentMission = ReadXML(XML_PLANET);
            return FormatString(currentMission, format);
        }

        public string GetSpearOfAdunAbility(int slot, Format format = Format.AsIs)
        {
            var soaAbility = ReadXML(XML_SOA[slot], SpearOfAdunBank);
            return FormatString(soaAbility, format);
        }

        public void SetSpearOfAdunAbility(int slot, string ability, Format format = Format.AsIs)
        {
            WriteXML(XML_SOA[slot], ability, SpearOfAdunBank);
        }

        private string ReadXML(string location)
        {
            return ReadXML(location, BattleNetBank);
        }

        private string ReadXML(string location, string bankFile)
        {
            if (_nodeTypes.ContainsKey(location))
            {
                return ReadXML(location, _nodeTypes[location], bankFile);
            }

            return null;
        }

        private string ReadXML(string location, string type, string bankFile)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(bankFile);
                var node = doc.DocumentElement.SelectSingleNode(location);
                var nodeAttribute = node.Attributes[type];
                return nodeAttribute.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public void WriteXML(string location, string value)
        {
            WriteXML(location, value, BattleNetBank);
        }

        public void WriteXML(string location, string value, string bankFile)
        {
            if (_nodeTypes.ContainsKey(location))
            {
                WriteXML(location, value, _nodeTypes[location], bankFile);
                return;
            }

            Console.WriteLine("Unidentified XML Node, not writing anything!");
        }

        public void WriteXML(string location, string value, string type, string bankFile) //This does not write XML and we need to adjust the path.
        {
            try
            {
                Console.WriteLine($"WRITEXML: I am writing {value} to {location} in {bankFile}");
                var doc = new XmlDocument();
                doc.Load(bankFile);
                var node = doc.DocumentElement.SelectSingleNode(location);
                var nodeAttribute = node.Attributes[type];
                nodeAttribute.InnerText = value;
                doc.Save(bankFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not write XML to file! (file in use) " + e.ToString());
                return;
            }
        }

        private string FormatString(string stringToFormat, Format format)
        {
            if (stringToFormat == null)
            {
                return "";
            }

            switch (format)
            {
                case Format.AsIs:
                    return stringToFormat;
                case Format.LowerCase:
                    return stringToFormat.ToLower();
                case Format.UpperCase:
                    return stringToFormat.ToUpper();
                case Format.TitleCase:
                    return MakeTitleCase(stringToFormat);
                case Format.LowerCaseNoSpaces:
                    return stringToFormat.ToLower().Replace(" ", "");
                case Format.UpperCaseNoSpaces:
                    return stringToFormat.ToUpper().Replace(" ", "");
                case Format.TitleCaseNoSpaces:
                    return MakeTitleCase(stringToFormat).Replace(" ", "");
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public static string MakeTitleCase(string stringToTitle)
        {
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(stringToTitle);
        }
    }
}
