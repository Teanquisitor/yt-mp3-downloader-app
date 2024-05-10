using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace YT_Downloader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] links;

        public MainWindow() => InitializeComponent();

        private void File_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var file = Path.GetFileName(files[0]);

                links = File.ReadAllLines(files[0]);

                input_field.Text = file;
                debug_log.Content = $"File {file} dropped!";
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (links != null)
            {
                foreach (var file in links)
                {
                    await Download(file);
                }

                links = null;
                input_field.Text = "";
                return;
            }

            await Download(input_field.Text);
            input_field.Text = "";
        }

        private async Task Download(string link)
        {
            try
            {
                debug_log.Content = "Started!";

                // Validate input URL
                if (!Uri.TryCreate(link, UriKind.Absolute, out _))
                {
                    debug_log.Content = "Aborted operation! Invalid URL.";
                    return;
                }

                // Download .webm file
                debug_log.Content = "Downloading...";
                await ExecuteCommand($"yt-dlp {link}");
                debug_log.Content = "Downloaded!";

                // Get .webm file
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.webm");
                var input = files[0];
                File.Move(input, "input.webm");

                // Convert .webm file to .mp3
                debug_log.Content = "Converting...";
                await ExecuteCommand($"ffmpeg -i \"input.webm\" -b:a 320k \"output.mp3\"");
                debug_log.Content = "Converted!";

                // Rename output file
                var finalName = Path.ChangeExtension($"{input}", ".mp3");
                File.Move("output.mp3", finalName);
                File.Delete("input.webm");

                debug_log.Content = "Finished!";
            }
            catch (WebException ex)
            {
                debug_log.Content = $"Web Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                debug_log.Content = $"Error: {ex.Message}";
            }
        }

        private Task<string> ExecuteCommand(string command)
        {
            return Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                process.StandardInput.WriteLine(command);
                process.StandardInput.Flush();
                process.StandardInput.Close();

                var output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                process.Close();

                return output;
            });
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState.Equals(e.LeftButton)) this.DragMove();
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e) => this.Close();

    }
}