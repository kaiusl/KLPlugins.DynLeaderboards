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
            if (FirstName == "") {
                return $"{LastName}";
            }
            return $"{FirstName[0]}. {LastName}";
        }

        public string Initials() {
            if (FirstName != "" && LastName != "") {
                return $"{FirstName[0]}{LastName[0]}";
            } else if (FirstName == "" && LastName != "") {
                return $"{LastName[0]}";
            } else if (FirstName != "" && LastName == "") {
                return $"{FirstName[0]}";
            } else {
                return "";
            }
        }
    }
}
