using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Tests.Helpers;

using Xunit;
using Xunit.Abstractions;

// ReSharper disable ClassNeverInstantiated.Global

namespace KLPlugins.DynLeaderboards.Tests.Integration.Settings;

public interface IDynLeaderboardFixture {
    DynLeaderboardsPlugin Ldb { get; }
    bool HasThrown { get; set; }
    int RunCount { get; set; }
}

public abstract class DynLeaderboardFixtureBase : IDisposable, IDynLeaderboardFixture {
    public bool HasThrown { get; set; } = false;
    public DynLeaderboardsPlugin Ldb { get; }

    private readonly string _oldWorkingDir;
    internal string _TmpDir { get; }

    public int RunCount { get; set; } = 0;

    public DynLeaderboardFixtureBase(string tmpDir, string? srcDir = null) {
        this._TmpDir = tmpDir;

        if (Directory.Exists(this._TmpDir)) {
            Directory.Delete(this._TmpDir, true);
        }

        Directory.CreateDirectory(this._TmpDir);
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
    protected int RunCount {
        get => fixture.RunCount;
        set => fixture.RunCount = value;
    }
    protected bool HasThrown {
        get => fixture.HasThrown;
        set => fixture.HasThrown = value;
    }
    private DynLeaderboardsPlugin _ldb => fixture.Ldb;

    // subclass pre-init tests, up to 99_999

    [Fact]
    [Order(100_000)]
    public void Init() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        this._ldb.InitCore(Game.ACC_NAME);

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public void CheckDirs() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        AssertMore.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b");
        AssertMore.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\laps_data");
        AssertMore.DirectoryExists("PluginsData\\KLPlugins\\DynLeaderboards\\Logs");

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task GeneralSettings() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.Verify(new TestPluginSettings(DynLeaderboardsPlugin._Settings));

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task DynLeaderboardsConfigs() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        const string THIS_METHOD = nameof(this.DynLeaderboardsConfigs);
        foreach (var ldb in DynLeaderboardsPlugin._Settings.DynLeaderboardConfigs) {
            await Verifier
                .Verify(ldb)
                .UseMethodName(THIS_METHOD + "_" + ldb.Name);
        }

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task CarInfos() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.Verify(DynLeaderboardsPlugin._Settings.Infos.CarInfos);

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task ClassInfos() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.Verify(
            DynLeaderboardsPlugin._Settings.Infos.ClassInfos.Select(c => c.MapKey(k => k.AsString()))
        );

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task TeamCupCategoryColors() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.Verify(
            DynLeaderboardsPlugin._Settings.Infos.TeamCupCategoryColors.Select(c => c.MapKey(k => k.AsString()))
        );

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(110_000)]
    public async Task DriverCategoryColors() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.Verify(
            DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors.Select(c => c.MapKey(k => k.AsString()))
        );

        this.HasThrown = prevHasThrown;
    }


    // subclass tests after 200_000

    [Fact]
    [Order(300_000)]
    public void End() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        this._ldb.End(null!);

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(310_000)]
    public void SettingsSaved() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        AssertMore.FileExists("PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json");
        AssertMore.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\CarInfos.json");
        AssertMore.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\ClassInfos.json");
        AssertMore.FileExists(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\DriverCategoryColors.json"
        );
        AssertMore.FileExists(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\TeamCupCategoryColors.json"
        );

        foreach (var ldb in this._ldb.DynLeaderboards) {
            AssertMore.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\" + ldb.Name + ".json");
        }

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedGeneralSettings() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.VerifyFile("PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json");

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedCarInfos() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.VerifyFile("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\CarInfos.json");

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedClassInfos() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.VerifyFile("PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\ClassInfos.json");

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedDriverCategoryColors() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.VerifyFile(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\DriverCategoryColors.json"
        );

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedTeamCupCategoryColors() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        await Verifier.VerifyFile(
            "PluginsData\\KLPlugins\\DynLeaderboards\\AssettoCorsaCompetizione\\TeamCupCategoryColors.json"
        );

        this.HasThrown = prevHasThrown;
    }

    [Fact]
    [Order(311_000)]
    public async Task SavedDynLeaderboardConfigs() {
        testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        const string THIS_METHOD = nameof(this.SavedDynLeaderboardConfigs);
        foreach (var ldb in DynLeaderboardsPlugin._Settings.DynLeaderboardConfigs) {
            await Verifier
                .VerifyFile("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\" + ldb.Name + ".json")
                .UseMethodName(THIS_METHOD + "_" + ldb.Name);
        }

        this.HasThrown = prevHasThrown;
    }

    // subclass post end tests from 400_000
}

internal class TestPluginSettings(PluginSettings inner) {
    public int Version => inner.Version;
    public string? AccDataLocation => inner.AccDataLocation;
    public string? AcRootLocation => inner.AcRootLocation;
    public bool Log => inner.Log;
    public OutGeneralProp OutGeneralProps => inner.OutGeneralProps.Value;
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

public class V1DynLeaderboardsPluginTests(
    V1InstallDynLeaderboardFixture fixture,
    ITestOutputHelper testOutputHelper
)
    : DynLeaderboardsPluginTestsCore(
            fixture,
            testOutputHelper
        ),
        IClassFixture<V1InstallDynLeaderboardFixture> {
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    [Order(200_000)]
    public void CheckSettingsMigration() {
        this._testOutputHelper.WriteLine((this.RunCount++).ToString());
        var prevHasThrown = this.HasThrown;
        this.HasThrown = true;

        AssertMore.FileExists("PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json.v2.bak");
        AssertMore.FileExists("PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b\\Dynamic.json.v2.bak");

        this.HasThrown = prevHasThrown;
    }
}

public class V2InstallDynLeaderboardFixture() : DynLeaderboardFixtureBase(
    ".\\V2InstallTestsTempDir",
    ".\\..\\..\\MockSimhubDirs\\V2.x"
);

public class V2DynLeaderboardsPluginTests(
    V2InstallDynLeaderboardFixture fixture,
    ITestOutputHelper testOutputHelper
)
    : DynLeaderboardsPluginTestsCore(
            fixture,
            testOutputHelper
        ),
        IClassFixture<V2InstallDynLeaderboardFixture> { }