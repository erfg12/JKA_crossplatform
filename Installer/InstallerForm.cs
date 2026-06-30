using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace Installer;
public partial class InstallerForm : Form
{
    private string installationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "JKACrossplatform"
        );

    public InstallerForm()
    {
        InitializeComponent();
    }

    private void InstallBtn_Click(object sender, EventArgs e)
    {
        InstallFiles();
    }

    void InstallFiles()
    {
        if (!Directory.Exists(installationPath))
            Directory.CreateDirectory(installationPath);

        var asm = Assembly.GetExecutingAssembly();
        string[] names = asm.GetManifestResourceNames();
        var prefix = "Installer."; // Adjust to your actual default namespace + folder

        foreach (var resName in asm.GetManifestResourceNames())
        {
            string relativePath = resName.Replace('/', Path.DirectorySeparatorChar);

            string outputPath = Path.Combine(installationPath, relativePath);
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                OutputBox.SelectionColor = Color.Green;
                OutputBox.AppendText($"Created directory: {Path.GetDirectoryName(outputPath)}\n");
                OutputBox.ScrollToCaret();
            }

            // If file exists, delete it
            if (System.IO.File.Exists(outputPath))
            {
                System.IO.File.Delete(outputPath);
                OutputBox.SelectionColor = Color.Red;
                OutputBox.AppendText($"Deleted existing file: {outputPath}\n");
                OutputBox.ScrollToCaret();
            }

            if (OutputBox.SelectionColor != Color.Green)
                OutputBox.SelectionColor = Color.Green;

            using var stream = asm.GetManifestResourceStream(resName);
            using var outFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            OutputBox.AppendText($"Created file: {outputPath}\n");
            OutputBox.ScrollToCaret();
            stream.CopyTo(outFile);
        }

        CreateShortcut();
        if (!CreateRegistry())
            return;

        OutputBox.SelectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
        OutputBox.SelectionColor = Color.Black; // optional
        OutputBox.AppendText($"INSTALLATION COMPLETE!");

        InstallBtn.Enabled = false;
    }


    void CreateShortcut()
    {
        OutputBox.SelectionColor = Color.Black;
        OutputBox.AppendText($"Creating desktop shortcuts.\n");
        OutputBox.ScrollToCaret();

        string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        using (StreamWriter writer = new StreamWriter($"{deskDir}\\JKACrossplatform.url"))
        {
            string app = Path.Combine($"{installationPath}", "JKACrossplatform.exe");
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine("URL=file:///" + app);
            writer.WriteLine("IconIndex=0");
            string icon = app.Replace('\\', '/');
            writer.WriteLine("IconFile=" + icon);
        }
    }

    bool CreateRegistry()
    {
        OutputBox.SelectionColor = Color.Black;
        OutputBox.AppendText($"Creating system registry...\n");
        OutputBox.ScrollToCaret();

        var key = Registry.CurrentUser.CreateSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\JKACrossplatform");

        if (!System.IO.File.Exists(Path.Combine(installationPath, "JKACrossplatform.exe")))
        {
            OutputBox.SelectionColor = Color.Red;
            OutputBox.AppendText($"ERROR: Cannot find JKACrossplatform.exe in installation path for registry entry.\n");
            OutputBox.ScrollToCaret();
            return false;
        }

        var version = FileVersionInfo.GetVersionInfo(Path.Combine(installationPath, "JKACrossplatform.exe")).FileVersion ?? "1.1.0.0";

        key.SetValue("DisplayName", "JKACrossplatform");
        key.SetValue("DisplayVersion", version);
        key.SetValue("Publisher", "Jacob Fliss");
        key.SetValue("InstallLocation", installationPath);
        key.SetValue("UninstallString", Path.Combine(installationPath, "Uninstall.exe")); // or custom uninstaller
        key.SetValue("DisplayIcon", Path.Combine(installationPath, "JKACrossplatform.exe"));

        return true;
    }

    private void InstallerForm_Shown(object sender, EventArgs e)
    {
        OutputBox.SelectionColor = Color.Goldenrod;
        OutputBox.AppendText($"Welcome to the JKACrossplatform Installer!\n\n");
        OutputBox.SelectionColor = Color.Blue;
        OutputBox.AppendText($"You can change the installation path using the bottom left control. It's not recommended to target the 'C:\\Program Files\\' directory!\n\n");

        InstallPathBox.Text = installationPath;
    }

    private void BrowseBtn_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            installationPath = dialog.SelectedPath;
            InstallPathBox.Text = installationPath;
        }
    }
}
