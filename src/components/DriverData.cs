using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork.Structs;

namespace KLPlugins.DynLeaderboards {
    public class DriverData {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string ShortName { get; private set; }
        public DriverCategory Category { get; private set; }
        public NationalityEnum Nationality { get; private set; }
        public int TotalLaps { get; private set; } = 0;
        public LapInfo BestSessionLap { get; private set; } = null;
        public string CategoryColor => DynLeaderboardsPlugin.Settings.DriverCategoryColors[Category];

        private double _totalDrivingTime = 0;

        internal DriverData(DriverInfo info) {
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

        internal void OnLapFinished(LapInfo lastLap) {
            TotalLaps++;
            if (BestSessionLap?.Laptime == null || (lastLap.IsValidForBest && BestSessionLap.Laptime > lastLap.Laptime)) {
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

    }
}
