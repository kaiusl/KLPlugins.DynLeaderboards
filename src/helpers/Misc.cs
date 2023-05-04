using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
            _watch = new Stopwatch();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            _file = File.Create(path);
            _writer = new StreamWriter(_file);
        }

        internal void Restart() {
            _watch.Restart();
        }

        internal void Stop() {
            _watch.Stop();
        }

        internal double Millis() {
            return _watch.Elapsed.TotalMilliseconds;
        }

        internal double Micros() {
            return _watch.Elapsed.TotalMilliseconds * 1_000.0;
        }

        internal double Nanos() {
            return _watch.Elapsed.TotalMilliseconds * 1_000_000.0;
        }

        internal void Write(double elapsed) {
            _writer?.WriteLine($"{elapsed}");
        }

        internal double StopAndWriteMicros() {
            Stop();
            var micros = Micros();
            Write(micros);
            return micros;
        }

        internal void Dispose() {
            _watch.Stop();
            if (_writer != null) {
                _writer.Dispose();
                _writer = null;
            }
            if (_file != null) {
                _file.Dispose();
                _file = null;
            }
        }
    }

    internal class Timers {

        private readonly Dictionary<string, Timer> _watches = new();
        private readonly string _rootPath;

        internal Timers(string rootPath) {
            _rootPath = rootPath;
            Directory.CreateDirectory(_rootPath);
            SimHub.Logging.Current.Info($"Created dir at {_rootPath}");
        }

        internal Timer Add(string name) {
            var path = $"{_rootPath}\\{name}\\{DynLeaderboardsPlugin.PluginStartTime}.txt";
            if (!_watches.ContainsKey(name)) {
                var timer = new Timer(path);
                _watches.Add(name, timer);
            }
            return _watches[name];
        }

        internal Timer AddAndRestart(string name) {
            var timer = Add(name);
            timer.Restart();
            return timer;
        }

        internal void Dispose() {
            foreach (var w in _watches) {
                w.Value.Dispose();
            }
            _watches.Clear();
        }
    }
}