using System.Diagnostics;

namespace ReignsAccess.Installer;

internal sealed class InstallerForm : Form
{
    private readonly GitHubReleaseClient _releaseClient = new();
    private readonly PackageInstaller _packageInstaller = new();
    private readonly CancellationTokenSource _lifetime = new();

    private readonly TextBox _gameDirectory = new();
    private readonly ComboBox _version = new();
    private readonly CheckBox _includeTestReleases = new();
    private readonly AccessibleStatusLabel _status = new();
    private readonly ProgressBar _progress = new();
    private readonly TextBox _log = new();
    private readonly Button _browse = new();
    private readonly Button _refresh = new();
    private readonly Button _install = new();
    private readonly Button _localPackage = new();
    private readonly Button _uninstall = new();
    private readonly Button _close = new();

    private IReadOnlyList<ModRelease> _allReleases = [];
    private bool _busy;
    private bool _disposedResources;

    public InstallerForm()
    {
        Text = "Reigns Access Installer";
        AccessibleName = Text;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 540);
        Size = new Size(820, 650);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildInterface();
        Shown += OnShown;
        FormClosing += OnFormClosing;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposedResources)
        {
            _disposedResources = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
            _releaseClient.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 9
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var introduction = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Text = "Install or update the screen-reader accessibility mod for the Steam version of Reigns. Close the game before continuing."
        };
        layout.Controls.Add(introduction);

        var gameLabel = new Label { AutoSize = true, Text = "&Reigns game folder:" };
        layout.Controls.Add(gameLabel);

        var gameRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        gameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        gameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _gameDirectory.Dock = DockStyle.Fill;
        _gameDirectory.AccessibleName = "Reigns game folder";
        _gameDirectory.Leave += (_, _) => UpdateInstallState();
        _browse.Text = "&Browse...";
        _browse.AutoSize = true;
        _browse.Click += BrowseGameDirectory;
        gameRow.Controls.Add(_gameDirectory, 0, 0);
        gameRow.Controls.Add(_browse, 1, 0);
        layout.Controls.Add(gameRow);

        var versionLabel = new Label { AutoSize = true, Text = "&Version:" };
        layout.Controls.Add(versionLabel);

        var versionRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        versionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        versionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        versionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _version.Dock = DockStyle.Fill;
        _version.DropDownStyle = ComboBoxStyle.DropDownList;
        _version.AccessibleName = "Mod version";
        _version.SelectedIndexChanged += (_, _) => UpdateInstallState();
        _includeTestReleases.AutoSize = true;
        _includeTestReleases.Text = "Include &test releases";
        _includeTestReleases.CheckedChanged += (_, _) => BindReleases();
        _refresh.AutoSize = true;
        _refresh.Text = "&Refresh";
        _refresh.Click += async (_, _) => await RefreshReleasesAsync();
        versionRow.Controls.Add(_version, 0, 0);
        versionRow.Controls.Add(_includeTestReleases, 1, 0);
        versionRow.Controls.Add(_refresh, 2, 0);
        layout.Controls.Add(versionRow);

        _status.AutoSize = true;
        _status.Padding = new Padding(0, 10, 0, 6);
        _status.Text = "Preparing installer...";
        _status.AccessibleName = "Installer status";
        layout.Controls.Add(_status);

        _progress.Dock = DockStyle.Top;
        _progress.AccessibleName = "Download and installation progress";
        layout.Controls.Add(_progress);

        var logLabel = new Label { AutoSize = true, Text = "Activity &log:" };
        layout.Controls.Add(logLabel);
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.AccessibleName = "Installer activity log";
        layout.Controls.Add(_log);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        _install.AutoSize = true;
        _install.Text = "&Install";
        _install.Click += async (_, _) => await InstallSelectedReleaseAsync();
        _localPackage.AutoSize = true;
        _localPackage.Text = "Install from local &ZIP...";
        _localPackage.Click += async (_, _) => await InstallLocalPackageAsync();
        _uninstall.AutoSize = true;
        _uninstall.Text = "&Uninstall mod";
        _uninstall.Click += UninstallMod;
        _close.AutoSize = true;
        _close.Text = "&Close";
        _close.Click += (_, _) => Close();
        buttons.Controls.AddRange([_install, _localPackage, _uninstall, _close]);
        layout.Controls.Add(buttons);

        Controls.Add(layout);
        AcceptButton = _install;
        CancelButton = _close;
    }

    private async void OnShown(object? sender, EventArgs eventArgs)
    {
        var detected = SteamLocator.FindReignsDirectory();
        if (detected is not null)
        {
            _gameDirectory.Text = detected;
            AppendLog($"Steam installation detected: {detected}");
        }
        else
        {
            AppendLog("Steam installation was not detected automatically. Use Browse to select it.");
        }

        UpdateInstallState();
        await RefreshReleasesAsync();
    }

    private async Task RefreshReleasesAsync()
    {
        if (_busy)
            return;

        SetBusy(true);
        SetStatus("Checking available versions on GitHub...");
        try
        {
            _allReleases = await _releaseClient.GetReleasesAsync(_lifetime.Token);
            BindReleases();
            AppendLog($"Found {_allReleases.Count} installable release(s).\r\n");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _allReleases = [];
            BindReleases();
            SetStatus("Could not load releases. Check your internet connection or install from a local ZIP.");
            AppendLog($"GitHub error: {exception.Message}");
        }
        finally
        {
            SetBusy(false);
            UpdateInstallState(keepErrorStatus: _allReleases.Count == 0);
        }
    }

    private void BindReleases()
    {
        var previousTag = (_version.SelectedItem as ModRelease)?.Tag;
        var visible = _allReleases
            .Where(release => _includeTestReleases.Checked || !release.IsPrerelease)
            .OrderByDescending(
                release => release.Tag,
                Comparer<string>.Create(ModVersionComparer.CompareReleaseTags))
            .ToArray();

        _version.BeginUpdate();
        _version.Items.Clear();
        _version.Items.AddRange(visible);
        _version.EndUpdate();

        var previous = visible.FirstOrDefault(release => release.Tag == previousTag);
        _version.SelectedItem = previous ?? visible.FirstOrDefault();
        UpdateInstallState();
    }

    private async Task InstallSelectedReleaseAsync()
    {
        if (_version.SelectedItem is not ModRelease release)
        {
            ShowError("Select a version to install.");
            return;
        }

        await RunOperationAsync(async (progress, cancellationToken) =>
        {
            await _packageInstaller.InstallReleaseAsync(
                release,
                NormalizedGameDirectory(),
                _releaseClient,
                progress,
                AppendLog,
                cancellationToken);
            SetStatus($"Reigns Access {release.Tag} is installed and ready. Start Reigns normally through Steam.");
        });
    }

    private async Task InstallLocalPackageAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select a complete Reigns Access package",
            Filter = "ZIP packages (*.zip)|*.zip|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await RunOperationAsync(async (progress, cancellationToken) =>
        {
            await _packageInstaller.InstallLocalArchiveAsync(
                dialog.FileName,
                NormalizedGameDirectory(),
                progress,
                AppendLog,
                cancellationToken);
            SetStatus("The local package is installed and ready. Start Reigns normally through Steam.");
        });
    }

    private void UninstallMod(object? sender, EventArgs eventArgs)
    {
        if (SteamLocator.ValidateGameDirectory(_gameDirectory.Text) is { } validationError)
        {
            ShowError(validationError);
            return;
        }

        var answer = MessageBox.Show(
            this,
            "Remove Reigns Access? BepInEx and shared screen-reader files will be kept in case another mod uses them.",
            "Uninstall Reigns Access",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            _packageInstaller.Uninstall(NormalizedGameDirectory(), AppendLog);
            SetStatus("Reigns Access was uninstalled. BepInEx was kept.");
            UpdateInstallState(keepErrorStatus: true);
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private async Task RunOperationAsync(Func<IProgress<int>, CancellationToken, Task> operation)
    {
        if (SteamLocator.ValidateGameDirectory(_gameDirectory.Text) is { } validationError)
        {
            ShowError(validationError);
            return;
        }

        SetBusy(true);
        _progress.Value = 0;
        var progress = new Progress<int>(value => _progress.Value = Math.Clamp(value, 0, 100));
        try
        {
            await operation(progress, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            AppendLog($"Error: {exception.Message}");
            ShowError(exception.Message);
        }
        finally
        {
            SetBusy(false);
            UpdateInstallState(keepErrorStatus: true);
        }
    }

    private void BrowseGameDirectory(object? sender, EventArgs eventArgs)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the Steam Reigns folder containing Reigns.exe",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            InitialDirectory = Directory.Exists(_gameDirectory.Text) ? _gameDirectory.Text : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gameDirectory.Text = dialog.SelectedPath;
            UpdateInstallState();
        }
    }

    private void UpdateInstallState(bool keepErrorStatus = false)
    {
        var validationError = SteamLocator.ValidateGameDirectory(_gameDirectory.Text);
        var release = _version.SelectedItem as ModRelease;
        var validGame = validationError is null;

        _install.Enabled = !_busy && validGame && release is not null;
        _localPackage.Enabled = !_busy && validGame;

        var installedVersion = validGame ? _packageInstaller.GetInstalledVersion(NormalizedGameDirectory()) : null;
        _uninstall.Enabled = !_busy && validGame && installedVersion is not null;

        if (_busy || keepErrorStatus)
            return;
        if (!validGame)
        {
            SetStatus(validationError!);
            return;
        }
        if (release is null)
        {
            SetStatus("No online version is selected. Refresh the list or install from a local ZIP.");
            return;
        }

        if (installedVersion is null)
        {
            _install.Text = $"&Install {release.Tag}";
            SetStatus($"Ready to install Reigns Access {release.Tag}.");
        }
        else
        {
            switch (ModVersionComparer.Compare(installedVersion, release.Tag))
            {
                case VersionRelation.Same:
                    _install.Text = $"&Reinstall {release.Tag}";
                    SetStatus($"Reigns Access {installedVersion} is installed. The selected version can be reinstalled.");
                    break;
                case VersionRelation.Newer:
                    _install.Text = $"&Update to {release.Tag}";
                    SetStatus($"Update available: {installedVersion} to {release.Tag}.");
                    break;
                case VersionRelation.Older:
                    _install.Text = $"&Downgrade to {release.Tag}";
                    SetStatus($"Installed: {installedVersion}. The selected version {release.Tag} is older.");
                    break;
                default:
                    _install.Text = $"&Update to {release.Tag}";
                    SetStatus($"An installed copy was found. Version {release.Tag} is available.");
                    break;
            }
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _gameDirectory.Enabled = !busy;
        _browse.Enabled = !busy;
        _version.Enabled = !busy;
        _includeTestReleases.Enabled = !busy;
        _refresh.Enabled = !busy;
        _close.Enabled = true;
        _close.Text = busy ? "&Cancel and close" : "&Close";
        UseWaitCursor = busy;
        if (busy)
        {
            _install.Enabled = false;
            _localPackage.Enabled = false;
            _uninstall.Enabled = false;
        }
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
        _status.AnnounceChange();
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private string NormalizedGameDirectory() => Path.GetFullPath(_gameDirectory.Text.Trim().Trim('"'));

    private void ShowError(string message)
    {
        SetStatus(message);
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (!_busy)
            return;

        var answer = MessageBox.Show(
            this,
            "An operation is still running. Cancel it and close the installer?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer == DialogResult.Yes)
            _lifetime.Cancel();
        else
            eventArgs.Cancel = true;
    }

    private sealed class AccessibleStatusLabel : Label
    {
        public void AnnounceChange() => AccessibilityNotifyClients(AccessibleEvents.NameChange, -1);
    }
}
