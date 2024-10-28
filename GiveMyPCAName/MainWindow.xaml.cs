using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace GiveMyPCAName
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ListItem[] items = { };
        string[] hostnames = { };

        public MainWindow()
        {
            InitializeComponent();

            CurrentName.Text = Dns.GetHostName();

            ScanNetworkBtn.IsEnabled = false;
            ReRollBtn.IsEnabled = false;
            RenameBtn.IsEnabled = false;

            items = JsonSerializer.Deserialize<ListItem[]>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Lists", "index.json")));

            for (int i = 0; i < items.Length; i++)
            {
                List.Items.Add(items[i].Name);
            }

            List.SelectedIndex = 0;
        }

        class ListItem
        {
            [JsonPropertyName("name")]
            [JsonInclude()]
            public string Name;

            [JsonPropertyName("file")]
            [JsonInclude()]
            public string File;
        }

        private void GMPCANBtn_Click(object sender, RoutedEventArgs e)
        {
            Information.Visibility = Visibility.Collapsed;
            Lists.Visibility = Visibility.Visible;

            ScanNetworkBtn.IsEnabled = true;
            ReRollBtn.IsEnabled = true;
            RenameBtn.IsEnabled = true;

            Roll();
        }

        private async void ScanNetworkBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanNetworkBtn.IsEnabled = false;
            ScanNetworkResults.Height = 300;
            Height += 300;

            Hostnames.Visibility = Visibility.Collapsed;

            string subnet = Microsoft.VisualBasic.Interaction.InputBox("What is your subnet?", "GiveMyPCAName", "192.168.1.");

            List<string> hostnames = await GetHostnamesAsync(subnet);
            this.hostnames = hostnames.ToArray();

            Hostnames.Items.Clear();

            for (int i = 0; i < hostnames.Count; i++)
            {
                Hostnames.Items.Add(hostnames[i]);
            }

            Hostnames.Visibility = Visibility.Visible;
        }

        private void ReRollBtn_Click(object sender, RoutedEventArgs e)
        {
            Roll();
        }

        private void RenameBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resultA = MessageBox.Show("Are you sure?", "GiveMyPCAName", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(resultA == MessageBoxResult.No)
            {
                return;
            }

            RegistryKey key = Registry.LocalMachine;
            string activeComputerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName";
            RegistryKey activeCmpName = key.CreateSubKey(activeComputerName);
            activeCmpName.SetValue("ComputerName", ManuallyEdit.Text);
            activeCmpName.Close();
            string computerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName";
            RegistryKey cmpName = key.CreateSubKey(computerName);
            cmpName.SetValue("ComputerName", ManuallyEdit.Text);
            cmpName.Close();
            string _hostName = "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters\\";
            RegistryKey hostName = key.CreateSubKey(_hostName);
            hostName.SetValue("Hostname", ManuallyEdit.Text);
            hostName.SetValue("NV Hostname", ManuallyEdit.Text);
            hostName.Close();

            MessageBoxResult resultB = MessageBox.Show("Hostname successfully changed!\nWould you like to restart now?", "GiveMyPCAName", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(resultB == MessageBoxResult.Yes)
            {
                Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "shutdown.exe",
                        CreateNoWindow = true,
                        Arguments = "-r -t 0"
                    }
                };
                
                process.Start();
            }
        }

        private void List_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void Roll()
        {
            string[] lines = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "Lists", items[List.SelectedIndex].File));
            Random r = new Random();

            LatestRoll.Text = lines[r.Next(0, lines.Length)];
            ManuallyEdit.Text = LatestRoll.Text;

            for (int i = 0; i < hostnames.Length; i++)
            {
                if (hostnames[i].ToLower() == LatestRoll.Text.ToLower())
                {
                    Roll();
                    return;
                }
            }

            if (AllUpper.IsChecked == true)
            {
                LatestRoll.Text = LatestRoll.Text.ToUpper();
                ManuallyEdit.Text = LatestRoll.Text;
            }

            if(BeginUpper.IsChecked == true)
            {
                char[] latest = LatestRoll.Text.ToCharArray();
                latest[0] = char.ToUpper(latest[0]);
                LatestRoll.Text = string.Join("", latest);
                ManuallyEdit.Text = LatestRoll.Text;
            }
        }

        private async Task<List<string>> GetHostnamesAsync(string subnet)
        {
            List<string> hostnames = new List<string>();
            List<Task> tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                string ipAddress = $"{subnet}{i}";
                tasks.Add(Task.Run(async () =>
                {
                    if (await PingHostAsync(ipAddress))
                    {
                        try
                        {
                            IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                            hostnames.Add($"{entry.HostName}");
                            Console.WriteLine("Found {0}", entry.HostName);
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine("SocketException occurred");
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return hostnames;
        }

        private async Task<bool> PingHostAsync(string ipAddress)
        {
            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = await ping.SendPingAsync(ipAddress, 100);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void SourceCode_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/MrBisquit/GiveMyPCAName/") { UseShellExecute = true });
        }

        private void Suggest_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/MrBisquit/GiveMyPCAName/") { UseShellExecute = true });
        }
    }
}