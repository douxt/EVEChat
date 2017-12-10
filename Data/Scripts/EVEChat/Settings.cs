using System;
using System.Collections.Generic;

namespace Douxt
{
    [Serializable]
    public class Settings
    {
        public ushort SalaryPerMinute = 60;
        public ushort SalaryMax = 10000;
        public ushort ResourcePerMinute = 300;
        public ushort ResourceMax = 20000;
        public ushort ResourceIncreasePerPlayerMinute = 150;
        public bool Enabled = true;
        public bool Debug = false;
        public ushort CleaningFrequency = 5;
        public ushort DelayBeforeTurningOn = 120;
        public ushort DistanceBeforeTurningOn = 400;
        public bool OnlyForStations = false;
        public bool OnlyWithZeroSpeed = true;
        public ushort LimitPerFaction = 30;
        public ushort LimitPerPlayer = 3;
        public ushort LimitGridSizes = 150;
        public ushort MotionShutdownDelay = 5;
        public bool BuildingNotAllowed = true;
        public bool IndestructibleNoBuilds = true;
        public bool IndestructibleGrindOwner = true;
        public List<long> Indestructible = new List<long>();
        public List<long> IndestructibleOverrideBuilds = new List<long>();
        public List<long> IndestructibleOverrideGrindOwner = new List<long>();
        public List<string> RedeemedCodes = new List<string>();
    }

}
