using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;

namespace KLPlugins.DynLeaderboards.Driver {
    class DriverData {
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string ShortName { get; internal set; }
        public DriverCategory Category { get; internal set; }
        public NationalityEnum Nationality { get; internal set; }
        public int TotalLaps { get; internal set; } = 0;
        public LapInfo BestSessionLap { get; internal set; } = null;
        public string CategoryColor => DynLeaderboardsPlugin.Settings.DriverCategoryColors[Category];

        private double _totalDrivingTime = 0;

        internal DriverData(DriverInfo info) {
            FirstName = info.FirstName;
            LastName = info.LastName;
            ShortName = info.ShortName;
            Category = info.Category;
            Nationality = info.Nationality;
        }

        internal void OnLapFinished(LapInfo lastLap) {
            TotalLaps++;
            if (BestSessionLap?.Laptime == null || lastLap.IsValidForBest && BestSessionLap.Laptime > lastLap.Laptime) {
                BestSessionLap = lastLap;
            }
        }

        internal void OnStintEnd(double lastStintTime) {
            _totalDrivingTime += lastStintTime;
        }

        internal double GetTotalDrivingTime(bool isDriving = false, double? currentStintTime = null) {
            if (isDriving && currentStintTime != null) return _totalDrivingTime + (double)currentStintTime;
            return _totalDrivingTime;
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

        public bool Equals(DriverInfo p) {
            if (p is null) {
                return false;
            }

            return FirstName == p.FirstName && LastName == p.LastName && ShortName == p.ShortName && Nationality == p.Nationality && Category == p.Category;
        }

    }
}
