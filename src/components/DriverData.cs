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
        public string CategoryColor => DynLeaderboardsPlugin.Settings.DriverCategoryColors[this.Category];

        private double _totalDrivingTime = 0;

        internal DriverData(in DriverInfo info) {
            this.FirstName = info.FirstName;
            this.LastName = info.LastName;
            this.ShortName = info.ShortName;
            this.Category = info.Category;
            this.Nationality = info.Nationality;

            this.FullName = this.FirstName + " " + this.LastName;
            this.InitialPlusLastName = this.CreateInitialPlusLastName();
            this.Initials = this.CreateInitials();
        }

        internal void OnLapFinished(in LapInfo lastLap) {
            this.TotalLaps++;
            var laptime = this.BestSessionLap?.Laptime;
            if (laptime == null || (lastLap.IsValidForBest && laptime > lastLap.Laptime)) {
                this.BestSessionLap = lastLap;
            }
        }

        internal void OnStintEnd(double lastStintTime) {
            this._totalDrivingTime += lastStintTime;
        }

        internal double GetTotalDrivingTime(bool isDriving = false, double? currentStintTime = null) {
            if (isDriving && currentStintTime != null) {
                return this._totalDrivingTime + (double)currentStintTime;
            }

            return this._totalDrivingTime;
        }

        private string CreateInitialPlusLastName() {
            if (this.FirstName == "") {
                return $"{this.LastName}";
            }
            return $"{this.FirstName[0]}. {this.LastName}";
        }

        private string CreateInitials() {
            if (this.FirstName != "" && this.LastName != "") {
                return $"{this.FirstName[0]}{this.LastName[0]}";
            } else if (this.FirstName == "" && this.LastName != "") {
                return $"{this.LastName[0]}";
            } else if (this.FirstName != "" && this.LastName == "") {
                return $"{this.FirstName[0]}";
            } else {
                return "";
            }
        }

        public bool Equals(in DriverInfo p) {
            return this.FirstName == p.FirstName
                    && this.LastName == p.LastName
                    && this.ShortName == p.ShortName
                    && this.Nationality == p.Nationality
                    && this.Category == p.Category;
        }
    }
}