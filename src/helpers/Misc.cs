using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace KLPlugins.DynLeaderboards.Helpers {

    internal static class Misc {

        public static bool EqualsAny<T>(this T lhs, params T[] rhs) {
            foreach (var v in rhs) {
                if (lhs.Equals(v)) {
                    return true;
                }
            }
            return false;
        }
    }

    class Timer {
        Stopwatch watch;
        FileStream? file;
        StreamWriter? writer;

        internal Timer(string path) {
            watch = new Stopwatch();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            file = File.Create(path);
            writer = new StreamWriter(file);
        }

        internal void Restart() {
            watch.Restart();
        }

        internal void Stop() {
            watch.Stop();
        }

        internal double Millis() {
            return watch.Elapsed.TotalMilliseconds;
        }

        internal double Micros() {
            return watch.Elapsed.TotalMilliseconds * 1_000.0;
        }

        internal double Nanos() {
            return watch.Elapsed.TotalMilliseconds * 1_000_000.0;
        }

        internal void Write(double elapsed) {
            writer?.WriteLine($"{elapsed}");
        }

        internal double StopAndWriteMicros() {
            Stop();
            var micros = Micros();
            Write(micros);
            return micros;
        }

        internal void Dispose() {
            watch.Stop();
            if (writer != null) {
                writer.Dispose();
                writer = null;
            }
            if (file != null) {
                file.Dispose();
                file = null;
            }
        }
    }

    internal class Timers {

        Dictionary<string, Timer> _watches = new Dictionary<string, Timer>();
        string _rootPath;

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