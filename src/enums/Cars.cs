using System;
using System.Linq;

using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;

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
        private readonly T[] _data;

        public EnumMap(Func<E, T> generator) {
            int count = Convert.ToInt32(Enum.GetValues(typeof(E)).Cast<E>().Max());
            this._data = new T[count + 1];

            foreach (var v in Enum.GetValues(typeof(E)).Cast<E>()) {
                int index = Convert.ToInt32(v);
                this._data[index] = generator(v);
            }
        }

        public T this[E key] => this._data[Convert.ToInt32(key)];
    }

    internal static class CarsMethods {
        internal static readonly EnumMap<CarType, CarClass> Classes = new(ClassGenerator);
        public static CarClass Class(this CarType c) {
            return Classes[c];
        }

        private static CarClass ClassGenerator(CarType c) {
            return c switch {
                CarType.Porsche991GT3R
                or CarType.MercedesAMGGT3
                or CarType.Ferrari488GT3
                or CarType.AudiR8LMSGT3
                or CarType.LamborghiniHuracanGT3
                or CarType.McLaren650SGT3
                or CarType.NissanGTRGT32018
                or CarType.BMWM6GT3
                or CarType.BentleyContinentalGT32018
                or CarType.NissanGTRGT32017
                or CarType.BentleyContinentalGT32016
                or CarType.AMRV12VantageGT3
                or CarType.LamborghiniGallardoREX
                or CarType.JaguarG3
                or CarType.LexusRCFGT3
                or CarType.LamborghiniHuracanGT3Evo
                or CarType.HondaNSXGT3
                or CarType.AudiR8LMSGT3Evo
                or CarType.AMRV8VantageGT3
                or CarType.HondaNSXGT3Evo
                or CarType.McLaren720SGT3
                or CarType.Porsche991IIGT3R
                or CarType.Ferrari488GT3Evo
                or CarType.MercedesAMGGT3Evo
                or CarType.BMWM4GT3
                or CarType.AudiR8LMSGT3Evo2
                or CarType.Ferrari296GT3
                or CarType.LamborghiniHuracanEvo2
                or CarType.Porsche992GT3R => CarClass.GT3,

                CarType.Ferrari488ChallengeEvo => CarClass.CHL,
                CarType.BMWM2CSRacing => CarClass.TCX,
                CarType.Porsche991IIGT3Cup => CarClass.CUP17,
                CarType.Porsche992GT3Cup => CarClass.CUP21,
                CarType.LamborghiniHuracanST2015 => CarClass.ST15,
                CarType.LamborghiniHuracanSTEvo2 => CarClass.ST21,

                CarType.AlpineA110GT4
                or CarType.AMRV8VantageGT4
                or CarType.AudiR8LMSGT4
                or CarType.BMWM4GT4
                or CarType.ChevroletCamaroGT4
                or CarType.GinettaG55GT4
                or CarType.KTMXbowGT4
                or CarType.MaseratiMCGT4
                or CarType.McLaren570SGT4
                or CarType.MercedesAMGGT4
                or CarType.Porsche718CaymanGT4 => CarClass.GT4,

                _ => CarClass.Unknown
            };
        }

        internal static readonly EnumMap<CarType, CarGroup> Groups = new(GroupGenerator);
        public static CarGroup Group(this CarType c) {
            return Groups[c];
        }

        private static CarGroup GroupGenerator(CarType c) {
            return c.Class() switch {
                CarClass.GT3 => CarGroup.GT3,
                CarClass.GT4 => CarGroup.GT4,
                CarClass.ST15
                or CarClass.ST21
                or CarClass.CUP17
                or CarClass.CUP21
                or CarClass.CHL => CarGroup.GTC,
                CarClass.TCX => CarGroup.TCX,
                _ => CarGroup.Unknown
            };
        }

        internal static readonly EnumMap<CarType, string> PrettyNames = new(PrettyNameGenerator);
        public static string PrettyName(this CarType c) {
            return PrettyNames[c];
        }

        private static string PrettyNameGenerator(CarType c) {
            return c switch {
                CarType.Porsche991GT3R => "Porsche 991 GT3 R",
                CarType.MercedesAMGGT3 => "Mercedes AMG-GT3",
                CarType.Ferrari488GT3 => "Ferrari 488 GT3",
                CarType.AudiR8LMSGT3 => "Audi R8 LMS GT3",
                CarType.LamborghiniHuracanGT3 => "Lamborghini Huracan GT3",
                CarType.McLaren650SGT3 => "McLaren 650S GT3",
                CarType.NissanGTRGT32018 => "Nissan GT-R GT3 18",
                CarType.BMWM6GT3 => "BMW M6 GT3",
                CarType.BentleyContinentalGT32018 => "Bentley Continental GT3 18",
                CarType.NissanGTRGT32017 => "Nissan GT-R GT3 17",
                CarType.BentleyContinentalGT32016 => "Bentley Continental GT3 16",
                CarType.AMRV12VantageGT3 => "Aston Martin V12 Vantage GT3",
                CarType.LamborghiniGallardoREX => "Lamborghini Gallardo REX",
                CarType.JaguarG3 => "Emil Frey Jaguar G3",
                CarType.LexusRCFGT3 => "Lexus RC-F GT3",
                CarType.LamborghiniHuracanGT3Evo => "Lamborghini Huracan GT3 EVO",
                CarType.HondaNSXGT3 => "Honda NSX GT3",
                CarType.AudiR8LMSGT3Evo => "Audi R8 LMS GT3 evo",
                CarType.AMRV8VantageGT3 => "Aston Martin V8 Vantage GT3",
                CarType.HondaNSXGT3Evo => "Honda NSX GT3 Evo",
                CarType.McLaren720SGT3 => "McLaren 720S GT3",
                CarType.Porsche991IIGT3R => "Porsche 991II GT3 R",
                CarType.Ferrari488GT3Evo => "Ferrari 488 GT3 EVO 2020",
                CarType.MercedesAMGGT3Evo => "Mercedes AMG-GT3 20",
                CarType.BMWM4GT3 => "BMW M4 GT3",
                CarType.AudiR8LMSGT3Evo2 => "Audi R8 LMS GT3 evo II",
                CarType.Ferrari296GT3 => "Ferrari 296 GT3",
                CarType.LamborghiniHuracanEvo2 => "Lamborghini Huracan GT3 EVO2",
                CarType.Porsche992GT3R => "Porsche 992 GT3 R",
                CarType.Ferrari488ChallengeEvo => "Ferrari 488 Challenge Evo",
                CarType.BMWM2CSRacing => "BMW M2 CS Racing",
                CarType.Porsche991IIGT3Cup => "Porsche 991II GT3 Cup",
                CarType.Porsche992GT3Cup => "Porsche 992 GT3 Cup",
                CarType.LamborghiniHuracanST2015 => "Lamborghini Huracan ST",
                CarType.LamborghiniHuracanSTEvo2 => "Lamborghini Huracan ST EVO2",
                CarType.AlpineA110GT4 => "Alpine A110 GT4",
                CarType.AMRV8VantageGT4 => "Aston Martin V8 Vantage GT4",
                CarType.AudiR8LMSGT4 => "Audi R8 LMS GT4",
                CarType.BMWM4GT4 => "BMW M4 GT4",
                CarType.ChevroletCamaroGT4 => "Chevrolet Camaro GT4",
                CarType.GinettaG55GT4 => "Ginetta G55 GT4",
                CarType.KTMXbowGT4 => "KTM X-Bow GT4",
                CarType.MaseratiMCGT4 => "Maserati MC GT4",
                CarType.McLaren570SGT4 => "McLaren 570S GT4",
                CarType.MercedesAMGGT4 => "Mercedes AMG-GT4",
                CarType.Porsche718CaymanGT4 => "Porsche 718 Cayman GT4",
                _ => "Unknown",
            };
        }

        internal static readonly EnumMap<CarType, string> Marks = new(MarkGenerator);
        public static string Mark(this CarType c) {
            return Marks[c];
        }

        private static string MarkGenerator(CarType c) {
            return c switch {
                CarType.Porsche991GT3R
                or CarType.Porsche991IIGT3R
                or CarType.Porsche991IIGT3Cup
                or CarType.Porsche992GT3Cup
                or CarType.Porsche718CaymanGT4
                or CarType.Porsche992GT3R => "Porsche",

                CarType.MercedesAMGGT3
                or CarType.MercedesAMGGT3Evo
                or CarType.MercedesAMGGT4 => "Mercedes",

                CarType.Ferrari488GT3
                or CarType.Ferrari488GT3Evo
                or CarType.Ferrari488ChallengeEvo
                or CarType.Ferrari296GT3 => "Ferrari",

                CarType.AudiR8LMSGT3
                or CarType.AudiR8LMSGT3Evo
                or CarType.AudiR8LMSGT3Evo2
                or CarType.AudiR8LMSGT4 => "Audi",

                CarType.LamborghiniHuracanGT3
                or CarType.LamborghiniGallardoREX
                or CarType.LamborghiniHuracanGT3Evo
                or CarType.LamborghiniHuracanST2015
                or CarType.LamborghiniHuracanSTEvo2
                or CarType.LamborghiniHuracanEvo2 => "Lamborghini",

                CarType.McLaren720SGT3
                or CarType.McLaren650SGT3
                or CarType.McLaren570SGT4 => "McLaren",

                CarType.NissanGTRGT32018
                or CarType.NissanGTRGT32017 => "Nissan",

                CarType.BMWM6GT3
                or CarType.BMWM4GT3
                or CarType.BMWM2CSRacing
                or CarType.BMWM4GT4 => "BMW",

                CarType.BentleyContinentalGT32018
                or CarType.BentleyContinentalGT32016 => "Bentley",

                CarType.AMRV12VantageGT3
                or CarType.AMRV8VantageGT3
                or CarType.AMRV8VantageGT4 => "Aston Martin",

                CarType.JaguarG3 => "Jaguar",
                CarType.LexusRCFGT3 => "Lexus",

                CarType.HondaNSXGT3
                or CarType.HondaNSXGT3Evo => "Honda",

                CarType.AlpineA110GT4 => "Alpine",
                CarType.ChevroletCamaroGT4 => "Chevrolet",
                CarType.GinettaG55GT4 => "Ginetta",
                CarType.KTMXbowGT4 => "KTM",
                CarType.MaseratiMCGT4 => "Maserati",
                _ => "Unknown",
            };
        }

        private static readonly EnumMap<CarClass, string> _accClassColors = new(ACCClassColorGenerator);
        public static string ACCColor(this CarClass c) {
            return _accClassColors[c];
        }

        private static string ACCClassColorGenerator(CarClass c) {
            return c switch {
                CarClass.GT3 => $"#FF000000",
                CarClass.GT4 => "#FF262660",
                CarClass.CUP17 => "#FF457C45",
                CarClass.CUP21 => "#FF284C28",
                CarClass.ST15 => "#FFCCBA00",
                CarClass.ST21 => "#FF988A00",
                CarClass.CHL => "#FFB90000",
                CarClass.TCX => "#FF007CA7",
                _ => "#FF000000",
            };
        }

        private static readonly EnumMap<CarClass, string> _carClassStrings = new((e) => e.ToString());
        public static string PrettyName(this CarClass c) {
            return _carClassStrings[c];
        }

        private static readonly EnumMap<TeamCupCategory, string> _teamCupCategoryStrings = new((e) => e.ToString());
        public static string PrettyName(this TeamCupCategory c) {
            return _teamCupCategoryStrings[c];
        }

        private static readonly EnumMap<TeamCupCategory, string> _accCupCategoryColors = new(ACCCupCategoryColorsGenerator);
        public static string ACCColor(this TeamCupCategory c) {
            return _accCupCategoryColors[c];
        }

        public static string ACCCupCategoryColorsGenerator(this TeamCupCategory c) {
            return c switch {
                TeamCupCategory.Overall => "#FFFFFFFF",
                TeamCupCategory.ProAm => "#FF000000",
                TeamCupCategory.Am => "#FFE80000",
                TeamCupCategory.Silver => "#FF666666",
                TeamCupCategory.National => "#FF008F4B",
                _ => "#FF000000",
            };
        }

        private static readonly EnumMap<TeamCupCategory, string> _accCupCategoryTextColors = new(ACCCupCategoryTextColorsGenerator);
        public static string ACCTextColor(this TeamCupCategory c) {
            return _accCupCategoryTextColors[c];
        }

        public static string ACCCupCategoryTextColorsGenerator(this TeamCupCategory c) {
            return c switch {
                TeamCupCategory.Overall => "#FF000000",
                TeamCupCategory.ProAm => "#FFFFFFFF",
                TeamCupCategory.Am => "#FF000000",
                TeamCupCategory.Silver => "#FFFFFFFF",
                TeamCupCategory.National => "#FFFFFFFF",
                _ => "#FF000000",
            };
        }
    }
}