using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace KLPlugins.DynLeaderboards.Common;

internal class Timer {
    private readonly Stopwatch _watch;
    private FileStream? _file;
    private StreamWriter? _writer;

    internal Timer(string path) {
        this._watch = new Stopwatch();
        var dir = Path.GetDirectoryName(path);
        if (dir == null) {
            throw new DirectoryNotFoundException("Failed to create timings directory.");
        }

        Directory.CreateDirectory(dir);
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

#if TIMINGS
internal static class Timers {
    private static readonly Dictionary<string, Timer> _watches = new();
    private static readonly string _rootPath = $"{PluginPaths._DataDir}\\Timings";
    private static readonly string _initTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";
    private static readonly Timer _selfGetTimer = Timers.Add("Timers.AddAndRestart");

    private static Timer Add(string name) {
        var path = $@"{Timers._rootPath}\{name}\{Timers._initTime}.txt";
        if (!Timers._watches.ContainsKey(name)) {
            var timer = new Timer(path);
            Timers._watches.Add(name, timer);
        }

        return Timers._watches[name];
    }

    internal static Timer AddOrGetAndRestart(string name) {
        Timers._selfGetTimer.Restart();
        var timer = Timers.Add(name);
        timer.Restart();
        Timers._selfGetTimer.StopAndWriteMicros();
        return timer;
    }

    internal static void Dispose() {
        foreach (var w in Timers._watches) {
            w.Value.Dispose();
        }

        Timers._watches.Clear();
    }
}
#endif