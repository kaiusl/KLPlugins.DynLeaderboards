using System;
using System.Collections.Generic;
using System.Linq;

namespace KLPlugins.DynLeaderboards.Common;

internal static class Extensions {
    public static bool EqualsAny<T>(this T lhs, params T[] rhs) {
        foreach (var v in rhs) {
            if (lhs == null || rhs == null) {
                continue;
            }

            if (lhs.Equals(v)) {
                return true;
            }
        }

        return false;
    }

    public static int ToInt(this bool v) {
        return v ? 1 : 0;
    }
}

internal static class EnumerableExtensions {
    public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> enumerable) {
        return enumerable.Select((v, i) => (v, i));
    }

    /// <returns>Index of the first item that matches the predicate, -1 if not found.</returns>
    public static int FirstIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
        foreach (var (item, i) in enumerable.WithIndex()) {
            if (predicate(item)) {
                return i;
            }
        }

        return -1;
    }

    public static T? FirstOr<T>(this IEnumerable<T> enumerable, T? defValue)
        where T : struct {
        try {
            return enumerable.First();
        } catch {
            return defValue;
        }
    }

    public static bool Contains<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
        foreach (var v in enumerable) {
            if (predicate(v)) {
                return true;
            }
        }

        return false;
    }
}

internal static class ListExtensions {
    public static void MoveElementAt<T>(this List<T> list, int from, int to) {
        var item = list[from];
        list.RemoveAt(from);
        list.Insert(to, item);
    }

    public static T? ElementAtOr<T>(this List<T> list, int index, T? defValue) {
        if (index < 0 || index >= list.Count) {
            return defValue;
        }

        return list[index];
    }
}

internal static class DictExtensions {
    public static V? GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key) {
        return dict.GetValueOr(key, default);
    }

    public static V? GetValueOr<K, V>(this Dictionary<K, V> dict, K key, V? defValue) {
        return dict.TryGetValue(key, out var val) ? val : defValue;
    }

    public static V GetOrAddValue<K, V>(this Dictionary<K, V> dict, K key, V defValue) {
        if (dict.TryGetValue(key, out var value)) {
            return value;
        }
        
        dict[key] = defValue;
        return defValue;
    }

    public static V GetOrAddValue<K, V>(this Dictionary<K, V> dict, K key, Func<V> valueBuilder) {
        if (dict.TryGetValue(key, out var value)) {
            return value;
        }

        var defValue = valueBuilder();
        dict[key] = defValue;
        return defValue;

    }

    public static void Merge<K, V>(this Dictionary<K, V> dict, Dictionary<K, V> other) {
        foreach (var kv in other) {
            dict[kv.Key] = kv.Value;
        }
    }
}

internal static class ColorTools {
    public static System.Windows.Media.Color FromHex(string hex) {
        if (hex.Length != 7 && hex.Length != 9) {
            throw new ArgumentException("Hex string must be 7 or 9 characters long", nameof(hex));
        }

        if (hex[0] != '#') {
            throw new ArgumentException("Hex string must start with #", nameof(hex));
        }

        if (hex.Length == 7) {
            return System.Windows.Media.Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16)
            );
        }

        return System.Windows.Media.Color.FromArgb(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16),
            Convert.ToByte(hex.Substring(7, 2), 16)
        );
    }
    
    public static double Lightness(string color) {
        // from https://stackoverflow.com/a/56678483
        var col = ColorTools.FromHex(color);
        var r = ColorTools.ToLinRgb(col.R / 255.0);
        var g = ColorTools.ToLinRgb(col.G / 255.0);
        var b = ColorTools.ToLinRgb(col.B / 255.0);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public static double LStar(string color) {
        // from https://stackoverflow.com/a/56678483
        var y = ColorTools.Lightness(color);
        if (y < 0.008856) {
            return y * 903.3;
        }

        return Math.Pow(y, 1.0 / 3.0) * 116.0 - 16.0;
    }

    public static string ComplementaryBlackOrWhite(string color) {
        var lstar = ColorTools.LStar(color);
        return lstar > 70 ? "#000000" : "#FFFFFF";
    }

    private static double ToLinRgb(double c) {
        // from https://stackoverflow.com/a/56678483
        if (c <= 0.04045) {
            return c / 12.92;
        }

        var step1 = (c + 0.055) / 1.055;
        return Math.Pow(step1, 2.4);
    }
}