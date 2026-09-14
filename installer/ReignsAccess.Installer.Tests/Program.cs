using System.IO.Compression;
using System.Reflection;
using ReignsAccess.Installer;

var tests = new (string Name, Action Run)[]
{
    ("Steam libraryfolders.vdf parsing", TestSteamLibraryParsing),
    ("Steam Reigns auto-detection", TestSteamDetection),
    ("Full release package selection", TestReleaseAssetSelection),
    ("Mod update version comparison", TestModVersionComparison),
    ("ZIP top-level folder removal", TestZipTopLevelFolder),
    ("ZIP traversal rejection", TestZipTraversalRejection),
    ("Accessible native controls", TestAccessibleControls),
    ("Generated release install and uninstall", TestGeneratedRelease)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {test.Name}: {exception.Message}");
    }
}

return failures == 0 ? 0 : 1;

static void TestSteamLibraryParsing()
{
    const string vdf = "\"libraryfolders\"\n{\n\"1\" { \"path\" \"D:\\\\SteamLibrary\" }\n\"2\" { \"path\" \"E:\\\\Games\\\\Steam\" }\n}";
    var paths = SteamLocator.ParseLibraryFolders(vdf);
    Assert(paths.SequenceEqual([@"D:\SteamLibrary", @"E:\Games\Steam"]), "Parsed paths were incorrect.");
}

static void TestSteamDetection()
{
    var detected = SteamLocator.FindReignsDirectory();
    Assert(detected is not null, "The installed Steam copy of Reigns was not detected.");
    Assert(File.Exists(Path.Combine(detected!, "Reigns.exe")), "Detected directory has no Reigns.exe.");
}

static void TestReleaseAssetSelection()
{
    var assets = new[]
    {
        new GitHubReleaseClient.GitHubAssetDto { Name = "Reigns-Access.v1.0.zip" },
        new GitHubReleaseClient.GitHubAssetDto { Name = "Reigns-Access.v1.0.with.BepInEx.zip" },
        new GitHubReleaseClient.GitHubAssetDto { Name = "ReignsAccessInstaller.exe" }
    };
    var selected = GitHubReleaseClient.SelectPackageAsset(assets);
    Assert(selected?.Name.Contains("BepInEx", StringComparison.OrdinalIgnoreCase) == true, "The complete bundle was not preferred.");
}

static void TestModVersionComparison()
{
    Assert(ModVersionComparer.Compare("v1.0.1", "v1.0.2") == VersionRelation.Newer, "A newer mod release was not recognized as an update.");
    Assert(ModVersionComparer.Compare("v1.0.2", "v1.0.1") == VersionRelation.Older, "An older mod release was not recognized as a downgrade.");
    Assert(ModVersionComparer.Compare("1.0.2", "v1.0.2") == VersionRelation.Same, "Equivalent version formats were not matched.");
    Assert(ModVersionComparer.Compare("v1.0.2-beta.1", "v1.0.2") == VersionRelation.Newer, "A stable release was not considered newer than its prerelease.");
    Assert(ModVersionComparer.Compare("manual/unknown", "v1.0.2") == VersionRelation.Unknown, "A manual installation should have an unknown version relation.");

    var ordered = new[] { "v1.0.2", "v1.2.0", "v1.1.9" }
        .OrderByDescending(tag => tag, Comparer<string>.Create(ModVersionComparer.CompareReleaseTags))
        .ToArray();
    Assert(ordered.SequenceEqual(["v1.2.0", "v1.1.9", "v1.0.2"]), "Release tags were not sorted newest first.");
}

static void TestZipTopLevelFolder()
{
    var root = Path.Combine(Path.GetTempPath(), "ReignsAccessInstallerTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        var archivePath = Path.Combine(root, "package.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "Reigns Access/BepInEx/plugins/ReignsAccess.dll", "test");
            WriteEntry(archive, "Reigns Access/.doorstop_version", ".");
        }

        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(output);
        var files = PackageInstaller.ExtractSafely(archivePath, output, CancellationToken.None);
        Assert(files.Contains(Path.Combine("BepInEx", "plugins", "ReignsAccess.dll")), "Top-level folder was not removed.");
        Assert(File.Exists(Path.Combine(output, "BepInEx", "plugins", "ReignsAccess.dll")), "File was not extracted to the expected location.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void TestZipTraversalRejection()
{
    var rejected = false;
    try
    {
        PackageInstaller.NormalizeArchivePath("../outside.dll");
    }
    catch (InvalidDataException)
    {
        rejected = true;
    }
    Assert(rejected, "A parent-directory traversal path was accepted.");
}

static void TestGeneratedRelease()
{
    var archivePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "release",
        "assets",
        "ReignsAccess-1.1-windows-with-BepInEx.zip");
    Assert(File.Exists(archivePath), "The generated 1.1 release package was not found.");

    var root = Path.Combine(Path.GetTempPath(), "ReignsAccessInstallerTests", Guid.NewGuid().ToString("N"));
    var steamApps = Path.Combine(root, "steamapps");
    var game = Path.Combine(steamApps, "common", "Reigns");
    Directory.CreateDirectory(Path.Combine(game, "Reigns_Data"));
    File.WriteAllBytes(Path.Combine(game, "Reigns.exe"), []);
    File.WriteAllText(Path.Combine(steamApps, "appmanifest_474750.acf"), "\"AppState\" { \"appid\" \"474750\" \"installdir\" \"Reigns\" }");

    try
    {
        var installer = new PackageInstaller();
        installer.InstallLocalArchiveAsync(
            archivePath,
            game,
            progress: null,
            log: _ => { },
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(File.Exists(Path.Combine(game, "BepInEx", "plugins", "ReignsAccess.dll")), "The mod DLL was not installed.");
        Assert(File.Exists(Path.Combine(game, PackageInstaller.ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar))), "The install manifest was not written.");

        installer.Uninstall(game, _ => { });
        Assert(!File.Exists(Path.Combine(game, "BepInEx", "plugins", "ReignsAccess.dll")), "The mod DLL was not removed.");
        Assert(File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.dll")), "Uninstall removed shared BepInEx files.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void TestAccessibleControls()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            using var form = new InstallerForm();
            var controls = Descendants(form).ToArray();
            Assert(controls.OfType<TextBox>().Any(control => control.AccessibleName == "Reigns game folder"), "The game path has no accessible name.");
            Assert(controls.OfType<ComboBox>().Any(control => control.AccessibleName == "Mod version"), "The version selector has no accessible name.");
            Assert(controls.OfType<TextBox>().Any(control => control.AccessibleName == "Installer activity log"), "The activity log has no accessible name.");
            Assert(controls.OfType<Button>().Any(control => control.Text.Contains("Install", StringComparison.OrdinalIgnoreCase)), "The install button was not found.");

            var close = controls.OfType<Button>().Single(control => control.Text.Contains("Close", StringComparison.OrdinalIgnoreCase));
            form.CreateControl();
            typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(close, [EventArgs.Empty]);
            Assert(form.IsDisposed, "The Close button did not dispose the main window.");
        }
        catch (Exception exception)
        {
            failure = exception;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure is not null)
        throw failure;
}

static IEnumerable<Control> Descendants(Control parent)
{
    foreach (Control child in parent.Controls)
    {
        yield return child;
        foreach (var descendant in Descendants(child))
            yield return descendant;
    }
}

static void WriteEntry(ZipArchive archive, string path, string contents)
{
    var entry = archive.CreateEntry(path);
    using var writer = new StreamWriter(entry.Open());
    writer.Write(contents);
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
