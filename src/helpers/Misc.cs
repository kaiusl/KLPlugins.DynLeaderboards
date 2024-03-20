using System;
using System.Collections.Generic;
using System.Collections;
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

    /// <summary>
    /// Helper class for mapping enums to custom values.
    /// 
    /// It's based on an array and uses the discriminants of the enum as indices.
    /// Thus it assumes couple of things about the enum.
    ///   * It's values are distinct (otherwise two variants map to same index).
    ///   * It's values preferably start at 0 and are contiguous (otherwise we waste space).
    /// </summary>
    internal class EnumMap<E, T> : IEnumerable<T> where E : Enum {
        private readonly int _dataLen;
        private readonly T[] _data;
        public Func<E, T> Generator { get; private set; }
#if DEBUG
        private static bool _hasWarned = false;
#endif
        public EnumMap(T defValue) : this(_ => defValue) { }

        public EnumMap(Func<E, T> generator) {
            var values = Enum.GetValues(typeof(E)).Cast<E>().Select(e => Convert.ToInt32(e));
            var maxValue = values.Max();
#if DEBUG
            if (!EnumMap<E, T>._hasWarned) {
                var minValue = values.Min();
                var numValues = values.Count();
                var distinctValues = values.Distinct().Count();
                if (minValue != 0 || numValues != distinctValues || maxValue != numValues - 1) {
                    SimHub.Logging.Current.Warn($"KLPlugins.DynLeaderboards:\n    EnumMap<{typeof(E)}, {typeof(T)}> uses not an ideal enum:\n    min={minValue}, max={maxValue}, values={numValues}, distinctValues={distinctValues}");
                }
                EnumMap<E, T>._hasWarned = true;
            }
#endif
            // +1 to account for 0 value
            this._dataLen = maxValue + 1;
            this._data = new T[this._dataLen];
            this.Generator = generator;

            foreach (var v in this.GetEnumValues()) {
                int index = Convert.ToInt32(v);
                this._data[index] = generator(v);
            }
        }

        public T this[E key] {
            get => this._data[Convert.ToInt32(key)];
            set => this._data[Convert.ToInt32(key)] = value;
        }

        public void Reset() {
            foreach (var v in this.GetEnumValues()) {
                int index = Convert.ToInt32(v);
                this._data[index] = this.Generator(v);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            foreach (var index in this.GetEnumIndices()) {
                yield return this._data[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        private IEnumerable<E> GetEnumValues() {
            return Enum.GetValues(typeof(E)).Cast<E>();
        }

        private IEnumerable<int> GetEnumIndices() {
            return Enum.GetValues(typeof(E)).Cast<E>().Select(v => Convert.ToInt32(v));
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
    }

}