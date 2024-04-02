namespace KLPlugins.DynLeaderboards.Track {

    internal enum TrackType {
        BrandsHatch = 1,
        Spa = 2,
        Monza = 3,
        Misano = 4,
        PaulRicard = 5,
        Silverstone = 6,
        Hugaroring = 7,
        Nurburgring = 8,
        Barcelona = 9,
        Zolder = 10,
        Zandvoort = 11,
        Kyalami = 12,
        Bathurst = 13,
        LagunaSeca = 14,
        Suzuka = 15,
        Snetterton = 16,
        OultonPark = 17,
        DoningtonPark = 18,
        Imola = 19,
        COTA = 21,
        Indianapolis = 22,
        WatkinsGlen = 23,
        Valencia = 24,
        RedBullRing = 25,
        Nurburgring24h = 26,

        Unknown = 27
    }

    internal static class TrackExtensions {

        public static double SplinePosOffset(this TrackType track) {
            return track switch {
                TrackType.Silverstone => 0.0209485,
                TrackType.Spa => 0.0036425,
                _ => 0,
            };
        }
    }
}