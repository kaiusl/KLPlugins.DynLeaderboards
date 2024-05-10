using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KLPlugins.DynLeaderboards.Helpers {

    internal static class Misc {

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
    }

    class Timer {
        private readonly Stopwatch _watch;
        private FileStream? _file;
        private StreamWriter? _writer;

        internal Timer(string path) {
            this._watch = new Stopwatch();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            this._file = File.Create(path);
            this._writer = new StreamWriter(this._file);
        }

        internal void Restart() {
            this._watch.Restart();
        }

        internal void Stop() {
            this._watch.Stop();
        }

        internal double Millis() {
            return this._watch.Elapsed.TotalMilliseconds;
        }

        internal double Micros() {
            return this._watch.Elapsed.TotalMilliseconds * 1_000.0;
        }

        internal double Nanos() {
            return this._watch.Elapsed.TotalMilliseconds * 1_000_000.0;
        }

        internal void Write(double elapsed) {
            this._writer?.WriteLine($"{elapsed}");
        }

        internal double StopAndWriteMicros() {
            this.Stop();
            var micros = this.Micros();
            this.Write(micros);
            return micros;
        }

        internal void Dispose() {
            this._watch.Stop();
            if (this._writer != null) {
                this._writer.Dispose();
                this._writer = null;
            }
            if (this._file != null) {
                this._file.Dispose();
                this._file = null;
            }
        }
    }

    internal class Timers {

        private readonly Dictionary<string, Timer> _watches = new();
        private readonly string _rootPath;

        internal Timers(string rootPath) {
            this._rootPath = rootPath;
            Directory.CreateDirectory(this._rootPath);
            SimHub.Logging.Current.Info($"Created dir at {this._rootPath}");
        }

        internal Timer Add(string name) {
            var path = $"{this._rootPath}\\{name}\\{DynLeaderboardsPlugin.PluginStartTime}.txt";
            if (!this._watches.ContainsKey(name)) {
                var timer = new Timer(path);
                this._watches.Add(name, timer);
            }
            return this._watches[name];
        }

        internal Timer AddAndRestart(string name) {
            var timer = this.Add(name);
            timer.Restart();
            return timer;
        }

        internal void Dispose() {
            foreach (var w in this._watches) {
                w.Value.Dispose();
            }
            this._watches.Clear();
        }
    }

    public static class IEnumerableExtensions {
        internal static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> enumerable) {
            return enumerable.Select((v, i) => (v, i));
        }

        /// <returns>Index of the first item that matches the predicate, -1 if not found.</returns>
        internal static int FirstIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
            foreach ((T item, int i) in enumerable.WithIndex()) {
                if (predicate(item)) {
                    return i;
                }
            }

            return -1;
        }

        internal static T? FirstOr<T>(this IEnumerable<T> enumerable, T? defValue) where T : struct {
            try {
                return enumerable.First();
            } catch {
                return defValue;
            }
        }

        internal static bool Contains<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
            foreach (var v in enumerable) {
                if (predicate(v)) {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class ListExtensions {
        internal static void MoveElementAt<T>(this List<T> list, int from, int to) {
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }

        internal static T? ElementAtOr<T>(this List<T> list, int index, T? defValue) {
            if (index < 0 || index >= list.Count) {
                return defValue;
            }

            return list[index];
        }
    }

    internal static class DictExtensions {
        internal static V? GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key) {
            return dict.GetValueOr(key, default);
        }

        internal static V? GetValueOr<K, V>(this Dictionary<K, V> dict, K key, V? defValue) {
            if (dict.ContainsKey(key)) {
                return dict[key];
            }

            return defValue;
        }

        internal static V GetOrAddValue<K, V>(this Dictionary<K, V> dict, K key, V defValue) {
            if (!dict.ContainsKey(key)) {
                dict[key] = defValue;
            }

            return dict[key];
        }

        internal static void Merge<K, V>(this Dictionary<K, V> dict, Dictionary<K, V> other) {
            foreach (var kv in other) {
                dict[kv.Key] = kv.Value;
            }
        }
    }

    internal static class WindowsMediaColorExtensions {
        internal static System.Windows.Media.Color FromHex(string hex) {
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
            } else {
                return System.Windows.Media.Color.FromArgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16),
                Convert.ToByte(hex.Substring(7, 2), 16)
                );
            }


        }
    }

}