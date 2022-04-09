using System;


namespace KLPlugins.Leaderboard {
    [Flags]
    public enum OutOrder {
        None = 0,
        InClassPositions = 1 << 1,
        RelativeOnTrackPositions = 1 << 2,

        RelativeOverallPositions = 1 << 3,
        PartialRelativeOverallPositions = 1 << 4,
        RelativeClassPositions = 1 << 5,
        PartialRelativeClassPositions = 1 << 6,

        FocusedCarPosition = 1 << 7,
        OverallBestLapPosition = 1 << 8,
        InClassBestLapPosition = 1 << 9,
    }

    static class OutOrderExtensions {
        public static bool Includes(this OutOrder p, OutOrder o) => (p & o) != 0;
        public static bool IncludesAny(this OutOrder p, params OutOrder[] others) {
            foreach (var o in others) {
                if (p.Includes(o)) { 
                    return true;
                }
            }
            return false;
        }
        public static bool IncludesAll(this OutOrder p, params OutOrder[] others) {
            foreach (var o in others) {
                if (!p.Includes(o)) {
                    return false;
                }
            }
            return true;
        }



        public static void Combine(ref this OutOrder p, OutOrder o) => p |= o;
        public static void Remove(ref this OutOrder p, OutOrder o) => p &= ~o;

        public static string ToPropName(this OutOrder p) {
            switch (p) {
                case OutOrder.None:
                    return "None";
                case OutOrder.InClassPositions:
                    return "InClass.5.OverallPosition";
                case OutOrder.RelativeOnTrackPositions:
                    return "Relative.5.OverallPosition";
                case OutOrder.RelativeOverallPositions:
                    return "RelativeOverall.5.OverallPosition";
                case OutOrder.PartialRelativeOverallPositions:
                    return "PartiaRelativeOverall.5.OverallPosition";
                case OutOrder.RelativeClassPositions:
                    return "RelativeClass.5.OverallPosition";
                case OutOrder.PartialRelativeClassPositions:
                    return "PartialRelativeClass.5.OverallPosition";
                case OutOrder.FocusedCarPosition:
                    return "Focused.OverallPosition";
                case OutOrder.OverallBestLapPosition:
                    return "Overall.BestLapCar.OverallPosition";
                case OutOrder.InClassBestLapPosition:
                    return "InClass.BestLapCar.OverallPosition";
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum variant");
            }
        }

        public static string ToolTipText(this OutOrder p) {
            switch (p) {
                case OutOrder.None:
                    return "None";
                case OutOrder.InClassPositions:
                    return @"Overall positions of cars in focused car's class. Used to create class leaderboards.
For car properties use JavaScript function ´InClass(pos, propname)´";
                case OutOrder.RelativeOnTrackPositions:
                    return @"Overall positions of closest cars on track. Used to create relative leaderboards.
For car properties use JavaScript function  ´Relative(pos, propname)´";
                case OutOrder.RelativeOverallPositions:
                    return "Overall positions relative to the focused cars.";
                case OutOrder.PartialRelativeOverallPositions:
                    return "Overall positions where some number of top positions is shown and after that relative positions to the focused.";
                case OutOrder.RelativeClassPositions:
                    return "Class positions relative to the focused car.";
                case OutOrder.PartialRelativeClassPositions:
                    return "Class positions where some number of top positions is shown and after that relative positions to the focused.";
                case OutOrder.FocusedCarPosition:
                    return @"Overall position of focused car.
For car properties use JavaScript function ´Focused(propname)´";
                case OutOrder.OverallBestLapPosition:
                    return @"Overall position of the overll best lap car.
For car properties use JavaScript function  ´OverallBestLapCar(propname)´.";
                case OutOrder.InClassBestLapPosition:
                    return @"Overall position of the class best lap car. 
For car properties use JavaScript function  ´InClassBestLapCar(propname)´.";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid enum variant {p}");
            }
        }
    }

}