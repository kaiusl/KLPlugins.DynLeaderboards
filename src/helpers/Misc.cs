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
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> enumerable) {
            return enumerable.Select((v, i) => (v, i));
        }

        /// <returns>Index of the first item that matches the predicate, -1 if not found.</returns>
        public static int FirstIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
            foreach ((T item, int i) in enumerable.WithIndex()) {
                if (predicate(item)) {
                    return i;
                }
            }

            return -1;
        }
    }

    public static class ListExtensions {
        public static void MoveElementAt<T>(this List<T> list, int from, int to) {
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }
    }

    public static class DictExtensions {
        public static V? GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key) {
            return dict.GetValueOr(key, default);
        }

        public static V? GetValueOr<K, V>(this Dictionary<K, V> dict, K key, V? defValue) {
            if (dict.ContainsKey(key)) {
                return dict[key];
            }

            return defValue;
        }

        public static void Merge<K, V>(this Dictionary<K, V> dict, Dictionary<K, V> other) {
            foreach (var kv in other) {
                dict[kv.Key] = kv.Value;
            }
        }
    }

}