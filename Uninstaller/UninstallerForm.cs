using System.Diagnostics;
using Microsoft.Win32;

namespace Uninstaller;
public partial class UninstallerForm : Form
{
    string installPath = string.Empty;
    bool uninstallSuccess = false;

    public UninstallerForm()
    {
        InitializeComponent();
    }

    private void UninstallerForm_Shown(object sender, EventArgs e)
    {
        OutputRichTextBox.AppendText("Join our Discord and let me know how I can make JKACrossplatform better.\n\nIf you're ready to uninstall, click the Uninstall button.\n\n");
    }

    private void DiscordBtn_Click(object sender, EventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://discord.gg/ddhGacy",
            UseShellExecute = true
        });
    }

    private void UninstallBtn_Click(object sender, EventArgs e)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\JKACrossplatform");

        if (key == null)
        {
            OutputRichTextBox.AppendText("ERROR: JKACrossplatform is not installed or registry key not found.\n");
            OutputRichTextBox.ScrollToCaret();
            return;
        }

        installPath = key?.GetValue("InstallLocation") as string ?? string.Empty;

        uninstallSuccess = UninstallProcess();

        if (uninstallSuccess)
        {
            DeleteDesktopShortcut();
            OutputRichTextBox.AppendText("Removing registry\n");
            OutputRichTextBox.ScrollToCaret();
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\JKACrossplatform", false);
            OutputRichTextBox.AppendText("Uninstallation process has completed.\n");
            OutputRichTextBox.ScrollToCaret();
            UninstallBtn.Enabled = false;
        }
    }

    private void UninstallerForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        SelfDeleteUninstaller();
    }

    bool UninstallProcess()
    {
        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
        {
            try
            {
                foreach (var file in Directory.GetFiles(installPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                        OutputRichTextBox.AppendText($"Deleted file: {file}\n");
                        OutputRichTextBox.ScrollToCaret();
                    }
                    catch (Exception ex)
                    {
                        OutputRichTextBox.AppendText($"Failed to delete file: {file} ({ex.Message})\n");
                        OutputRichTextBox.ScrollToCaret();
                    }
                }

                foreach (var dir in Directory.GetDirectories(installPath, "*", SearchOption.AllDirectories).Reverse())
                {
                    try
                    {
                        Directory.Delete(dir);
                        OutputRichTextBox.AppendText($"Deleted folder: {dir}\n");
                        OutputRichTextBox.ScrollToCaret();
                    }
                    catch (Exception ex)
                    {
                        OutputRichTextBox.AppendText($"Failed to delete folder: {dir} ({ex.Message})\n");
                        OutputRichTextBox.ScrollToCaret();
                    }
                }
            }
            catch (Exception ex)
            {
                OutputRichTextBox.AppendText($"ERROR: Uninstall failed: {ex.Message}\n");
                return false;
            }
        }
        else
        {
            OutputRichTextBox.AppendText($"ERROR: Uninstall failed: Installation path not found!\n");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Deletes the uninstaller executable and root path after the uninstallation is complete by delaying the process by 2 seconds.
    /// </summary>
    void SelfDeleteUninstaller()
    {
        string uninstallerPath = Application.ExecutablePath;

        if (!uninstallSuccess || string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
            return;

        string cmd = $"/C ping 127.0.0.1 -n 2 > nul && rmdir /s /q \"{installPath}\" && del \"{uninstallerPath}\"";

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = cmd,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });
    }

    void DeleteDesktopShortcut()
    {
        string shortcutName = "JKACrossplatform.url"; // match the name you used when creating it
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string shortcutPath = Path.Combine(desktopPath, shortcutName);

        if (File.Exists(shortcutPath))
        {
            try
            {
                File.Delete(shortcutPath);
                OutputRichTextBox.AppendText($"Deleted shortcut: {shortcutPath}\n");
            }
            catch (Exception ex)
            {
                OutputRichTextBox.AppendText($"Failed to delete shortcut: {ex.Message}\n");
            }
        }
    }
}
