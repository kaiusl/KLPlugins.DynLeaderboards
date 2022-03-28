using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs
{
    public class DriverInfo
    {
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string ShortName { get; internal set; }
        public DriverCategory Category { get; internal set; }
        public NationalityEnum Nationality { get; internal set; }

        public string FullName() { 
            return FirstName + " " + LastName;
        }

        public string InitialPlusLastName() {
            return $"{FirstName[0]}. {LastName}";
        }

        public string Initials() {
            return $"{FirstName[0]}{LastName[0]}";
        }
    }
}
