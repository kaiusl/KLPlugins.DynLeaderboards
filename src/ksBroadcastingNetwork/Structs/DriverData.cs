using KLPlugins.Leaderboard.ksBroadcastingNetwork;
using KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KLPlugins.Leaderboard {
    public class DriverData : IEquatable<DriverInfo> {
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string ShortName { get; internal set; }
        public DriverCategory Category { get; internal set; }
        public NationalityEnum Nationality { get; internal set; }
        public double TotalDrivingTime { get; internal set; }
        public int TotalLaps { get; internal set; }
        public LapInfo BestSessionLap { get; internal set; }
        public double LastStintTime { get; internal set; }
        public int LapsInLastStint { get; internal set; }

        public DriverData(DriverInfo info) { 
            FirstName = info.FirstName;
            LastName = info.LastName;
            ShortName = info.ShortName;
            Category = info.Category;
            Nationality = info.Nationality;
        }

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

        public override bool Equals(object obj) => this.Equals(obj as DriverInfo);

        public bool Equals(DriverInfo p) {
            if (p is null) {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, p)) {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType()) {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (FirstName == p.FirstName) && (LastName == p.LastName) && (ShortName == p.ShortName) && (Nationality == p.Nationality) && (Category == p.Category);
        }

        public override int GetHashCode() => (FirstName, LastName, ShortName, Nationality, Category).GetHashCode();

        public static bool operator ==(DriverData lhs, DriverInfo rhs) {
            if (lhs is null) {
                if (rhs is null) {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DriverData lhs, DriverInfo rhs) => !(lhs == rhs);

    }
}
