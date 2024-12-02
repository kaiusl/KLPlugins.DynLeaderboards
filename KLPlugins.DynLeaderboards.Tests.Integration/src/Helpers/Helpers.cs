using System;
using System.IO;

namespace KLPlugins.DynLeaderboards.Tests.Helpers;

internal static class DirTools {
    internal static void DoInTmpDir(string dirName, Action action, string? srcDir = null) {
        if (Directory.Exists(dirName)) {
            Directory.Delete(dirName, true);
        }

        Directory.CreateDirectory(dirName);
        var oldWorkingDir = Directory.GetCurrentDirectory();
        try {
            if (srcDir != null) {
                DirTools.CopyDirectory(srcDir, dirName);
            }

            Directory.SetCurrentDirectory(dirName);

            action();
        } finally {
            Directory.SetCurrentDirectory(oldWorkingDir);
        }

        // Don't delete dir on exceptions so that we can actually go in and look what may have gone wrong
        Directory.Delete(dirName, true);
    }


    // from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
    internal static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true) {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists) {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles()) {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive) {
            foreach (var subDir in dirs) {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                DirTools.CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}