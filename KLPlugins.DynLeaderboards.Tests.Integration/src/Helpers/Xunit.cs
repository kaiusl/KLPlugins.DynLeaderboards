using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using DiffEngine;

using VerifyTests;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace KLPlugins.DynLeaderboards.Tests.Helpers;

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

internal static class AssertMore {
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

/// <summary>
///     Wrapper around VerifyXunit.Verifier to have some custom default settings,
///     because  [ModuleInitializer] is not available in .net-framework 4.8 and to apply global settings
///     we use a static constructor and wrap verify methods.
///     Also some setting cannot be applied globally.
/// </summary>
internal static class Verifier {
    static Verifier() {
        VerifierSettings.SortPropertiesAlphabetically();
        VerifierSettings.ScrubLinesWithReplace(
            replaceLine: l => l.Contains(Environment.UserName) ? l.Replace(Environment.UserName, "UserName") : l
        );
        DiffRunner.Disabled = true;
    }


    internal static SettingsTask Verify(
        object? v,
        VerifySettings? settings = null,
        [CallerFilePath] string? callerPath = null
    ) {
        return VerifyXunit.Verifier.Verify(v, Verifier.CreateSettings(settings, callerPath));
    }

    internal static SettingsTask VerifyFile(
        string path,
        VerifySettings? settings = null,
        [CallerFilePath] string? callerPath = null
    ) {
        return VerifyXunit.Verifier.VerifyFile(path, Verifier.CreateSettings(settings, callerPath));
    }

    private static VerifySettings CreateSettings(VerifySettings? settings = null, string? callerPath = null) {
        settings ??= new VerifySettings();
        var root = callerPath != null ? Path.GetDirectoryName(callerPath) : "..";
        settings.UseDirectory(root + "\\" + "snapshots");

        return settings;
    }
}