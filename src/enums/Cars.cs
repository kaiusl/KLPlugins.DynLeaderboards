using System;
using System.Linq;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using KLPlugins.DynLeaderboards.Settings;

namespace KLPlugins.DynLeaderboards.Car {

    public enum CarType {
        Porsche991GT3R = 0,
        MercedesAMGGT3 = 1,
        Ferrari488GT3 = 2,
        AudiR8LMSGT3 = 3,
        LamborghiniHuracanGT3 = 4,
        McLaren650SGT3 = 5,
        NissanGTRGT32018 = 6,
        BMWM6GT3 = 7,
        BentleyContinentalGT32018 = 8,
        Porsche991IIGT3Cup = 9,
        NissanGTRGT32017 = 10,
        BentleyContinentalGT32016 = 11,
        AMRV12VantageGT3 = 12,
        LamborghiniGallardoREX = 13,
        JaguarG3 = 14,
        LexusRCFGT3 = 15,
        LamborghiniHuracanGT3Evo = 16,
        HondaNSXGT3 = 17,
        LamborghiniHuracanST2015 = 18,
        AudiR8LMSGT3Evo = 19,
        AMRV8VantageGT3 = 20,
        HondaNSXGT3Evo = 21,
        McLaren720SGT3 = 22,
        Porsche991IIGT3R = 23,
        Ferrari488GT3Evo = 24,
        MercedesAMGGT3Evo = 25,
        Ferrari488ChallengeEvo = 26,
        BMWM2CSRacing = 27,
        Porsche992GT3Cup = 28,
        LamborghiniHuracanSTEvo2 = 29,
        BMWM4GT3 = 30,
        AudiR8LMSGT3Evo2 = 31,
        Ferrari296GT3 = 32,
        LamborghiniHuracanEvo2 = 33,
        Porsche992GT3R = 34,

        AlpineA110GT4 = 50,
        AMRV8VantageGT4 = 51,
        AudiR8LMSGT4 = 52,
        BMWM4GT4 = 53,
        ChevroletCamaroGT4 = 55,
        GinettaG55GT4 = 56,
        KTMXbowGT4 = 57,
        MaseratiMCGT4 = 58,
        McLaren570SGT4 = 59,
        MercedesAMGGT4 = 60,
        Porsche718CaymanGT4 = 61,

        Unknown = 62
    }

    public enum CarClass {
        Overall = 0,
        GT3 = 1,
        GT4 = 2,
        ST15 = 3,
        ST21 = 4,
        CHL = 5,
        CUP17 = 6,
        CUP21 = 7,
        TCX = 8,
        Unknown = 9
    }

    public enum CarGroup {
        Overall = 0,
        GT3 = 1,
        GT4 = 2,
        GTC = 3,
        TCX = 4,
        Unknown = 5
    }

    internal class EnumMap<E, T> where E : System.Enum {
        private T[] _data;

        public EnumMap(Func<E, T> generator) {
            int count = Convert.ToInt32(Enum.GetValues(typeof(E)).Cast<E>().Max());
            _data = new T[count + 1];

            foreach (var v in Enum.GetValues(typeof(E)).Cast<E>()) {
                int index = Convert.ToInt32(v);
                _data[index] = generator(v);
            }
        }

        public T this[E key] {
            get => _data[Convert.ToInt32(key)];
        }
    }

    internal static class CarsMethods {
        internal static readonly EnumMap<CarType, CarClass> Classes = new EnumMap<CarType, CarClass>(ClassGenerator);
        public static CarClass Class(this CarType c) {
            return Classes[c];
        }

        private static CarClass ClassGenerator(CarType c) {
            switch (c) {
                case CarType.Porsche991GT3R:
                case CarType.MercedesAMGGT3:
                case CarType.Ferrari488GT3:
                case CarType.AudiR8LMSGT3:
                case CarType.LamborghiniHuracanGT3:
                case CarType.McLaren650SGT3:
                case CarType.NissanGTRGT32018:
                case CarType.BMWM6GT3:
                case CarType.BentleyContinentalGT32018:
                case CarType.NissanGTRGT32017:
                case CarType.BentleyContinentalGT32016:
                case CarType.AMRV12VantageGT3:
                case CarType.LamborghiniGallardoREX:
                case CarType.JaguarG3:
                case CarType.LexusRCFGT3:
                case CarType.LamborghiniHuracanGT3Evo:
                case CarType.HondaNSXGT3:
                case CarType.AudiR8LMSGT3Evo:
                case CarType.AMRV8VantageGT3:
                case CarType.HondaNSXGT3Evo:
                case CarType.McLaren720SGT3:
                case CarType.Porsche991IIGT3R:
                case CarType.Ferrari488GT3Evo:
                case CarType.MercedesAMGGT3Evo:
                case CarType.BMWM4GT3:
                case CarType.AudiR8LMSGT3Evo2:
                case CarType.Ferrari296GT3:
                case CarType.LamborghiniHuracanEvo2:
                case CarType.Porsche992GT3R:
                    return CarClass.GT3;

                case CarType.Ferrari488ChallengeEvo:
                    return CarClass.CHL;

                case CarType.BMWM2CSRacing:
                    return CarClass.TCX;

                case CarType.Porsche991IIGT3Cup:
                    return CarClass.CUP17;

                case CarType.Porsche992GT3Cup:
                    return CarClass.CUP21;

                case CarType.LamborghiniHuracanST2015:
                    return CarClass.ST15;

                case CarType.LamborghiniHuracanSTEvo2:
                    return CarClass.ST21;

                case CarType.AlpineA110GT4:
                case CarType.AMRV8VantageGT4:
                case CarType.AudiR8LMSGT4:
                case CarType.BMWM4GT4:
                case CarType.ChevroletCamaroGT4:
                case CarType.GinettaG55GT4:
                case CarType.KTMXbowGT4:
                case CarType.MaseratiMCGT4:
                case CarType.McLaren570SGT4:
                case CarType.MercedesAMGGT4:
                case CarType.Porsche718CaymanGT4:
                    return CarClass.GT4;

                default:
                    return CarClass.Unknown;
            }
        }

        internal static readonly EnumMap<CarType, CarGroup> Groups = new EnumMap<CarType, CarGroup>(GroupGenerator);
        public static CarGroup Group(this CarType c) {
            return Groups[c];
        }

        private static CarGroup GroupGenerator(CarType c) {
            switch (c.Class()) {
                case CarClass.GT3:
                    return CarGroup.GT3;

                case CarClass.GT4:
                    return CarGroup.GT4;

                case CarClass.ST15:
                case CarClass.ST21:
                case CarClass.CUP17:
                case CarClass.CUP21:
                case CarClass.CHL:
                    return CarGroup.GTC;

                case CarClass.TCX:
                    return CarGroup.TCX;

                default:
                    return CarGroup.Unknown;
            }
        }


        internal static readonly EnumMap<CarType, string> PrettyNames = new EnumMap<CarType, string>(PrettyNameGenerator);
        public static string PrettyName(this CarType c) {
            return PrettyNames[c];
        }

        private static string PrettyNameGenerator(CarType c) {
            switch (c) {
                case CarType.Porsche991GT3R:
                    return "Porsche 991 GT3 R";

                case CarType.MercedesAMGGT3:
                    return "Mercedes AMG-GT3";

                case CarType.Ferrari488GT3:
                    return "Ferrari 488 GT3";

                case CarType.AudiR8LMSGT3:
                    return "Audi R8 LMS GT3";

                case CarType.LamborghiniHuracanGT3:
                    return "Lamborghini Huracan GT3";

                case CarType.McLaren650SGT3:
                    return "McLaren 650S GT3";

                case CarType.NissanGTRGT32018:
                    return "Nissan GT-R GT3 18";

                case CarType.BMWM6GT3:
                    return "BMW M6 GT3";

                case CarType.BentleyContinentalGT32018:
                    return "Bentley Continental GT3 18";

                case CarType.NissanGTRGT32017:
                    return "Nissan GT-R GT3 17";

                case CarType.BentleyContinentalGT32016:
                    return "Bentley Continental GT3 16";

                case CarType.AMRV12VantageGT3:
                    return "Aston Martin V12 Vantage GT3";

                case CarType.LamborghiniGallardoREX:
                    return "Lamborghini Gallardo REX";

                case CarType.JaguarG3:
                    return "Emil Frey Jaguar G3";

                case CarType.LexusRCFGT3:
                    return "Lexus RC-F GT3";

                case CarType.LamborghiniHuracanGT3Evo:
                    return "Lamborghini Huracan GT3 EVO";

                case CarType.HondaNSXGT3:
                    return "Honda NSX GT3";

                case CarType.AudiR8LMSGT3Evo:
                    return "Audi R8 LMS GT3 evo";

                case CarType.AMRV8VantageGT3:
                    return "Aston Martin V8 Vantage GT3";

                case CarType.HondaNSXGT3Evo:
                    return "Honda NSX GT3 Evo";

                case CarType.McLaren720SGT3:
                    return "McLaren 720S GT3";

                case CarType.Porsche991IIGT3R:
                    return "Porsche 991II GT3 R";

                case CarType.Ferrari488GT3Evo:
                    return "Ferrari 488 GT3 EVO 2020";

                case CarType.MercedesAMGGT3Evo:
                    return "Mercedes AMG-GT3 20";

                case CarType.BMWM4GT3:
                    return "BMW M4 GT3";

                case CarType.AudiR8LMSGT3Evo2:
                    return "Audi R8 LMS GT3 evo II";

                case CarType.Ferrari296GT3:
                    return "Ferrari 296 GT3";

                case CarType.LamborghiniHuracanEvo2:
                    return "Lamborghini Huracan GT3 EVO2";

                case CarType.Porsche992GT3R:
                    return "Porsche 992 GT3 R";

                case CarType.Ferrari488ChallengeEvo:
                    return "Ferrari 488 Challenge Evo";

                case CarType.BMWM2CSRacing:
                    return "BMW M2 CS Racing";

                case CarType.Porsche991IIGT3Cup:
                    return "Porsche 991II GT3 Cup";

                case CarType.Porsche992GT3Cup:
                    return "Porsche 992 GT3 Cup";

                case CarType.LamborghiniHuracanST2015:
                    return "Lamborghini Huracan ST";

                case CarType.LamborghiniHuracanSTEvo2:
                    return "Lamborghini Huracan ST EVO2";

                case CarType.AlpineA110GT4:
                    return "Alpine A110 GT4";

                case CarType.AMRV8VantageGT4:
                    return "Aston Martin V8 Vantage GT4";

                case CarType.AudiR8LMSGT4:
                    return "Audi R8 LMS GT4";

                case CarType.BMWM4GT4:
                    return "BMW M4 GT4";

                case CarType.ChevroletCamaroGT4:
                    return "Chevrolet Camaro GT4";

                case CarType.GinettaG55GT4:
                    return "Ginetta G55 GT4";

                case CarType.KTMXbowGT4:
                    return "KTM X-Bow GT4";

                case CarType.MaseratiMCGT4:
                    return "Maserati MC GT4";

                case CarType.McLaren570SGT4:
                    return "McLaren 570S GT4";

                case CarType.MercedesAMGGT4:
                    return "Mercedes AMG-GT4";

                case CarType.Porsche718CaymanGT4:
                    return "Porsche 718 Cayman GT4";

                default:
                    return "Unknown";
            }
        }


        internal static readonly EnumMap<CarType, string> Marks = new EnumMap<CarType, string>(MarkGenerator);
        public static string Mark(this CarType c) {
            return Marks[c];
        }

        private static string MarkGenerator(CarType c) {
            switch (c) {
                case CarType.Porsche991GT3R:
                case CarType.Porsche991IIGT3R:
                case CarType.Porsche991IIGT3Cup:
                case CarType.Porsche992GT3Cup:
                case CarType.Porsche718CaymanGT4:
                case CarType.Porsche992GT3R:
                    return "Porsche";

                case CarType.MercedesAMGGT3:
                case CarType.MercedesAMGGT3Evo:
                case CarType.MercedesAMGGT4:
                    return "Mercedes";

                case CarType.Ferrari488GT3:
                case CarType.Ferrari488GT3Evo:
                case CarType.Ferrari488ChallengeEvo:
                case CarType.Ferrari296GT3:
                    return "Ferrari";

                case CarType.AudiR8LMSGT3:
                case CarType.AudiR8LMSGT3Evo:
                case CarType.AudiR8LMSGT3Evo2:
                case CarType.AudiR8LMSGT4:
                    return "Audi";

                case CarType.LamborghiniHuracanGT3:
                case CarType.LamborghiniGallardoREX:
                case CarType.LamborghiniHuracanGT3Evo:
                case CarType.LamborghiniHuracanST2015:
                case CarType.LamborghiniHuracanSTEvo2:
                case CarType.LamborghiniHuracanEvo2:
                    return "Lamborghini";

                case CarType.McLaren720SGT3:
                case CarType.McLaren650SGT3:
                case CarType.McLaren570SGT4:
                    return "McLaren";

                case CarType.NissanGTRGT32018:
                case CarType.NissanGTRGT32017:
                    return "Nissan";

                case CarType.BMWM6GT3:
                case CarType.BMWM4GT3:
                case CarType.BMWM2CSRacing:
                case CarType.BMWM4GT4:
                    return "BMW";

                case CarType.BentleyContinentalGT32018:
                case CarType.BentleyContinentalGT32016:
                    return "Bentley";

                case CarType.AMRV12VantageGT3:
                case CarType.AMRV8VantageGT3:
                case CarType.AMRV8VantageGT4:
                    return "Aston Martin";

                case CarType.JaguarG3:
                    return "Jaguar";

                case CarType.LexusRCFGT3:
                    return "Lexus";

                case CarType.HondaNSXGT3:
                case CarType.HondaNSXGT3Evo:
                    return "Honda";

                case CarType.AlpineA110GT4:
                    return "Alpine";

                case CarType.ChevroletCamaroGT4:
                    return "Chevrolet";

                case CarType.GinettaG55GT4:
                    return "Ginetta";

                case CarType.KTMXbowGT4:
                    return "KTM";

                case CarType.MaseratiMCGT4:
                    return "Maserati";

                default:
                    return "Unknown";
            }
        }

        private static readonly CarClassArray<string> ACCClassColors = new CarClassArray<string>(ACCClassColorGenerator);
        public static string ACCColor(this CarClass c) {
            return ACCClassColors[c];
        }

        private static string ACCClassColorGenerator(CarClass c) {
            switch (c) {
                case CarClass.GT3:
                    return $"#FF000000";

                case CarClass.GT4:
                    return "#FF262660";

                case CarClass.CUP17:
                    return "#FF457C45";

                case CarClass.CUP21:
                    return "#FF284C28";

                case CarClass.ST15:
                    return "#FFCCBA00";

                case CarClass.ST21:
                    return "#FF988A00";

                case CarClass.CHL:
                    return "#FFB90000";

                case CarClass.TCX:
                    return "#FF007CA7";

                default:
                    return "#FF000000";
            }
        }

        private static readonly EnumMap<TeamCupCategory, string> ACCCupCategoryColors = new EnumMap<TeamCupCategory, string>(ACCCupCategoryColorsGenerator);
        public static string ACCColor(this TeamCupCategory c) {
           return ACCCupCategoryColors[c];
        }

        public static string ACCCupCategoryColorsGenerator(this TeamCupCategory c) {
            switch (c) {
                case TeamCupCategory.Overall:
                    return "#FFFFFFFF";

                case TeamCupCategory.ProAm:
                    return "#FF000000";

                case TeamCupCategory.Am:
                    return "#FFE80000";

                case TeamCupCategory.Silver:
                    return "#FF666666";

                case TeamCupCategory.National:
                    return "#FF008F4B";

                default:
                    return "#FF000000";
            }
        }

        private static readonly EnumMap<TeamCupCategory, string> ACCCupCategoryTextColors = new EnumMap<TeamCupCategory, string>(ACCCupCategoryTextColorsGenerator);
        public static string ACCTextColor(this TeamCupCategory c) {
            return ACCCupCategoryTextColors[c];
        }

        public static string ACCCupCategoryTextColorsGenerator(this TeamCupCategory c) {
            switch (c) {
                case TeamCupCategory.Overall:
                    return "#FF000000";

                case TeamCupCategory.ProAm:
                    return "#FFFFFFFF";

                case TeamCupCategory.Am:
                    return "#FF000000";

                case TeamCupCategory.Silver:
                    return "#FFFFFFFF";

                case TeamCupCategory.National:
                    return "#FFFFFFFF";

                default:
                    return "#FF000000";
            }
        }
    }
}