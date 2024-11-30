using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace KLPlugins.DynLeaderboards.Tests.Helpers {
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
                    DirTools.CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }

// from https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit
    [AttributeUsage(AttributeTargets.Method)]
    public class OrderAttribute(uint priority) : Attribute {
        public uint Priority { get; } = priority;
    }

// from https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit
    public class PriorityOrderer : ITestCaseOrderer {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase {
            var assemblyName = typeof(OrderAttribute).AssemblyQualifiedName!;
            var sortedMethods = new SortedDictionary<uint, List<TTestCase>>();
            foreach (var testCase in testCases) {
                var priority = testCase.TestMethod.Method
                        .GetCustomAttributes(assemblyName)
                        .FirstOrDefault()
                        ?.GetNamedArgument<uint>(nameof(OrderAttribute.Priority))
                    ?? 0;

                PriorityOrderer.GetOrCreate(sortedMethods, priority).Add(testCase);
            }

            foreach (var testCase in
                sortedMethods.Keys.SelectMany(
                    priority => sortedMethods[priority].OrderBy(testCase => testCase.TestMethod.Method.Name)
                )) {
                yield return testCase;
            }
        }

        private static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : struct
            where TValue : new() {
            return dictionary.TryGetValue(key, out var result)
                ? result
                : dictionary[key] = new TValue();
        }
    }

    namespace XunitExtensions {
        internal static class Assert {
            internal static void True(bool condition, string? message = null) {
                Xunit.Assert.True(condition, message);
            }

            internal static void False(bool condition, string? message = null) {
                Xunit.Assert.False(condition, message);
            }

            internal static void Equal<T>(T lhs, T rhs) {
                Xunit.Assert.Equal(lhs, rhs);
            }

            internal static void FileExists(string path, string? message = null) {
                if (File.Exists(path)) {
                    return;
                }

                if (message != null) {
                    throw new FileExistsException(message);
                }

                throw FileExistsException.NotExists(path);
            }

            internal static void DirectoryExists(string path, string? message = null) {
                if (Directory.Exists(path)) {
                    return;
                }

                if (message != null) {
                    throw new DirectoryExistsException(message);
                }

                throw DirectoryExistsException.NotExists(path);
            }
        }

        internal class FileExistsException(string? message) : XunitException(message) {
            internal static FileExistsException NotExists(string path) {
                return new FileExistsException($"File does not exist: {path}");
            }
        }

        internal class DirectoryExistsException(string? message) : XunitException(message) {
            internal static DirectoryExistsException NotExists(string path) {
                return new DirectoryExistsException($"File does not exist: {path}");
            }
        }
    }
}