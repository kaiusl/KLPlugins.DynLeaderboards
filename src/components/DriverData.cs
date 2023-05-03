using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;

namespace KLPlugins.DynLeaderboards.Driver {

    internal class DriverData {
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string ShortName { get; internal set; }
        public string FullName { get; internal set; }
        public string InitialPlusLastName { get; internal set; }
        public string Initials { get; internal set; }

        public DriverCategory Category { get; internal set; }
        public NationalityEnum Nationality { get; internal set; }
        public int TotalLaps { get; internal set; } = 0;
        public LapInfo? BestSessionLap { get; internal set; } = null;
        public string CategoryColor => DynLeaderboardsPlugin.Settings.DriverCategoryColors[Category];

        private double _totalDrivingTime = 0;

        internal DriverData(in DriverInfo info) {
            FirstName = info.FirstName;
            LastName = info.LastName;
            ShortName = info.ShortName;
            Category = info.Category;
            Nationality = info.Nationality;

            FullName = FirstName + " " + LastName;
            InitialPlusLastName = CreateInitialPlusLastName();
            Initials = CreateInitials();
        }

        internal void OnLapFinished(in LapInfo lastLap) {
            TotalLaps++;
            var laptime = BestSessionLap?.Laptime;
            if (laptime == null || lastLap.IsValidForBest && laptime > lastLap.Laptime) {
                BestSessionLap = lastLap;
            }
        }

        internal void OnStintEnd(double lastStintTime) {
            _totalDrivingTime += lastStintTime;
        }

        internal double GetTotalDrivingTime(bool isDriving = false, double? currentStintTime = null) {
            if (isDriving && currentStintTime != null)
                return _totalDrivingTime + (double)currentStintTime;
            return _totalDrivingTime;
        }

        private string CreateInitialPlusLastName() {
            if (FirstName == "") {
                return $"{LastName}";
            }
            return $"{FirstName[0]}. {LastName}";
        }

        private string CreateInitials() {
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

        public bool Equals(in DriverInfo p) {
            return FirstName == p.FirstName 
                    && LastName == p.LastName 
                    && ShortName == p.ShortName 
                    && Nationality == p.Nationality 
                    && Category == p.Category;
        }
    }
}