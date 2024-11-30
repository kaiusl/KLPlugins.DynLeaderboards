using System;
using System.IO;

using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Tests.Helpers;

using Xunit;
using Xunit.Abstractions;

using Assert = KLPlugins.DynLeaderboards.Tests.Helpers.XunitExtensions.Assert;

// ReSharper disable ClassNeverInstantiated.Global

namespace KLPlugins.DynLeaderboards.Tests.Integration.Settings;

public interface IDynLeaderboardFixture {
    DynLeaderboardsPlugin Ldb { get; }
    bool HasThrown { get; set; }
}

public abstract class DynLeaderboardFixtureBase : IDisposable, IDynLeaderboardFixture {
    public bool HasThrown { get; set; } = false;
    public DynLeaderboardsPlugin Ldb { get; }

    private readonly string _oldWorkingDir;
    internal string _TmpDir { get; }

    public DynLeaderboardFixtureBase(string tmpDir, string? srcDir = null) {
        this._TmpDir = tmpDir;

        if (Directory.Exists(this._TmpDir)) {
            Directory.Delete(this._TmpDir, true);
        }

        Directory.CreateDirectory(this._TmpDir);
        var cwd = Directory.GetCurrentDirectory();
        DirTools.CopyDirectory(".\\..\\..\\MockSimhubDirs\\SimHubBase", this._TmpDir);
        if (srcDir != null) {
            DirTools.CopyDirectory(srcDir, this._TmpDir);
        }

        this._oldWorkingDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(this._TmpDir);

        this.Ldb = new DynLeaderboardsPlugin();
    }

    public void Dispose() {
        Directory.SetCurrentDirectory(this._oldWorkingDir);

        if (!this.HasThrown) {
            // Don't delete dir on exceptions so that we can actually go in and look what may have gone 
            Directory.Delete(this._TmpDir, true);
        }
    }
}

[TestSubject(typeof(DynLeaderboardsPlugin))]
[TestCaseOrderer(
    ordererTypeName: "KLPlugins.DynLeaderboards.Tests.Helpers.PriorityOrderer",
    ordererAssemblyName: "KLPlugins.DynLeaderboards.Tests.Integration"
)]
public abstract class DynLeaderboardsPluginTestsCore(
    IDynLeaderboardFixture fixture,
    ITestOutputHelper testOutputHelper
) {
    protected static int RunCount = 0;
    protected bool HasThrown {
        get => fixture.HasThrown;
        set => fixture.HasThrown = value;
    }
    private DynLeaderboardsPlugin _ldb => fixture.Ldb;

    // subclass pre-init tests, up to 99_999

    [Fact]
    [Order(100_000)]
    public void Init() {
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        // Assert.Equal(0, CleanInstallTests._runCount);
        testOutputHelper.WriteLine((DynLeaderboardsPluginTestsCore.RunCount++).ToString());
        //
        this._ldb.InitCore(Game.ACC_NAME);

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(100_001)]
    public void CheckDirs() {
        testOutputHelper.WriteLine((DynLeaderboardsPluginTestsCore.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        Assert.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b");
        Assert.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\laps_data");
        Assert.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\Logs");

        this.HasThrown = prevHasThrown;
    }

    // subclass tests after 200_000

    [Fact]
    [Order(300_000)]
    public void End() {
        testOutputHelper.WriteLine((DynLeaderboardsPluginTestsCore.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        this._ldb.End(null!);

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(300_001)]
    public void SettingsSaved() {
        testOutputHelper.WriteLine((DynLeaderboardsPluginTestsCore.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        Assert.FileExists("PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json");
        Assert.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\CarInfos.json");
        Assert.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\ClassInfos.json");
        Assert.FileExists(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\DriverCategoryColors.json"
        );
        Assert.FileExists(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\TeamCupCategoryColors.json"
        );

        foreach (var ldb in this._ldb.DynLeaderboards) {
            Assert.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\" + ldb.Name + ".json");
        }

        this.HasThrown = prevHasThrown;
    }

    // subclass post end tests from 400_000
}

public class CleanInstallDynLeaderboardFixture() : DynLeaderboardFixtureBase(".\\CleanInstallTestsTempDir");

public class CleanInstallDynLeaderboardsPluginTests(
    CleanInstallDynLeaderboardFixture fixture,
    ITestOutputHelper testOutputHelper
)
    : DynLeaderboardsPluginTestsCore(
            fixture,
            testOutputHelper
        ),
        IClassFixture<CleanInstallDynLeaderboardFixture>;

public class V1InstallDynLeaderboardFixture() : DynLeaderboardFixtureBase(
    ".\\V1InstallTestsTempDir",
    ".\\..\\..\\MockSimhubDirs\\V1.x"
);

public class V1DynLeaderboardsPluginTests(V1InstallDynLeaderboardFixture fixture, ITestOutputHelper testOutputHelper)
    : DynLeaderboardsPluginTestsCore(
            fixture,
            testOutputHelper
        ),
        IClassFixture<V1InstallDynLeaderboardFixture> {
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    [Order(200_000)]
    public void CheckSettingsMigration() {
        this._testOutputHelper.WriteLine((DynLeaderboardsPluginTestsCore.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        Assert.FileExists("PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json.v2.bak");
        Assert.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b\\Dynamic.json.v2.bak");

        this.HasThrown = prevHasThrown;
    }
}

public class V2InstallDynLeaderboardFixture() : DynLeaderboardFixtureBase(
    ".\\V2InstallTestsTempDir",
    ".\\..\\..\\MockSimhubDirs\\V2.x"
);

public class V2DynLeaderboardsPluginTests(V2InstallDynLeaderboardFixture fixture, ITestOutputHelper testOutputHelper)
    : DynLeaderboardsPluginTestsCore(
            fixture,
            testOutputHelper
        ),
        IClassFixture<V2InstallDynLeaderboardFixture> { }