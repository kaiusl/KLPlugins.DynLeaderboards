using System;


namespace KLPlugins.Leaderboard {
    [Flags]
    public enum OutOrder {
        None = 0,
        InClassPositions = 1 << 1,
        RelativePositions = 1 << 2,
        FocusedCarPosition = 1 << 3,
        OverallBestLapPosition = 1 << 4,
        InClassBestLapPosition = 1 << 5,
    }

    static class OutOrderExtensions {
        public static bool Includes(this OutOrder p, OutOrder o) => (p & o) != 0;
        public static void Combine(ref this OutOrder p, OutOrder o) => p |= o;
        public static void Remove(ref this OutOrder p, OutOrder o) => p &= ~o;

        public static string ToPropName(this OutOrder p) {
            switch (p) {
                case OutOrder.None:
                    return "None";
                case OutOrder.InClassPositions:
                    return "InClass.xx.OverallPosition";
                case OutOrder.RelativePositions:
                    return "Relative.xx.OverallPosition";
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
                case OutOrder.RelativePositions:
                    return @"Overall positions of closest cars on track. Used to create relative leaderboards.
For car properties use JavaScript function  ´Relative(pos, propname)´";
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