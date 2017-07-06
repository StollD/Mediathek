using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MediaLibrary
{
    /// <summary>
    /// Interaktionslogik für AddImageWindow.xaml
    /// </summary>
    public partial class AddImageWindow : Window
    {
        /// <summary>
        /// Referenz zum Hauptfenster
        /// </summary>
        private MainWindow Main { get; set; }

        public AddImageWindow(MainWindow main)
        {
            InitializeComponent();
            Main = main;
            Closing += delegate (Object sender, CancelEventArgs e) { e.Cancel = runner != null; };
            file_1.IsEnabled = file_2.IsEnabled = false;

            // CD Laufwerke erfassen
            List<String> sources_values = new List<String>();
            foreach (DriveInfo disk in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.CDRom))
            {
                if (!disk.IsReady)
                    continue;
                sources_values.Add(disk.VolumeLabel + " [" + disk.Name + "]");
            }
            if (sources_values.Any())
            {
                sources.ItemsSource = sources_values;
            }
            else
            {
                sources.IsEnabled = false;
                title.IsEnabled = false;
                type.IsEnabled = false;
                file_1.IsEnabled = false;
                file_2.IsEnabled = false;
                add.IsEnabled = false;
                progress.IsEnabled = false;
                categories.IsEnabled = false;
            }

            // Kategorien erfassen
            List<String> temp = MainWindow.Categories.ToList();
            temp.Insert(0, "Alle");
            categories.ItemsSource = temp;

            // Typen erfassen
            List<String> type_values = new List<String>();
            foreach (DiskType c in MainWindow.Types.Values)
            {
                type_values.Add(c.Name);
            }
            if (type_values.Any())
            {
                type.ItemsSource = type_values;
            }
            else
            {
                sources.IsEnabled = false;
                title.IsEnabled = false;
                type.IsEnabled = false;
                file_1.IsEnabled = false;
                file_2.IsEnabled = false;
                add.IsEnabled = false;
                progress.IsEnabled = false;
                categories.IsEnabled = false;
            }
        }

        public Task runner;

        /// <summary>
        /// Die DVD hinzufügen.
        /// </summary>
        private async void add_Click(Object sender, RoutedEventArgs e)
        {
            if (sources.SelectedValue == null || String.IsNullOrWhiteSpace(title.Text) || type.SelectedValue == null || String.IsNullOrWhiteSpace(file_1.Text) || categories.SelectedValue == null)
            {
                return;
            }
            String volumename = sources.SelectedValue.ToString().Split('[')[0].Trim();
            progress.IsEnabled = true;
            progress.IsIndeterminate = true;
            sources.IsEnabled = false;
            title.IsEnabled = false;
            type.IsEnabled = false;
            file_1.IsEnabled = false;
            file_2.IsEnabled = false;
            categories.IsEnabled = false;
            add.IsEnabled = false;
            runner = AddDVD(volumename, title.Text, type.SelectedValue.ToString(), categories.SelectedValue.ToString(), Path.GetFileName(file_1.Text));
            await runner;
            runner = null;
            Close();
            Main.ScanNetworkPath(Main.categories.SelectedValue?.ToString() ?? "Alle", Main.searchbar.Text?.Trim() ?? "");
        }

        private async Task AddDVD(String volumename, String dvdtitle, String type, String category, String file)
        {
            await Task.Run(() =>
            {
                Guid ID = Guid.NewGuid(); // Eindeutige ID für den Ordner
                String dir = Path.Combine(MainWindow.NetworkPath, ID.ToString());
                Directory.CreateDirectory(dir);

                // Typ herausfinden
                KeyValuePair<String, DiskType> c = MainWindow.Types.First(cat => cat.Value.Name == type);

                // Entry Datei schreiben
                File.WriteAllText(Path.Combine(dir, "entry.json"),
                    "{\n    \"Name\": \"" + dvdtitle + "\",\n    \"Type\": \"" + c.Key + "\",\n    \"File\": \"" + (c.Value.Command.Contains("{1}") ? file : "") + "\",\n    \"Category\": \"" + category + "\"\n}");

                // DVD Kopieren
                DriveInfo info = DriveInfo.GetDrives().First(d => d.VolumeLabel == volumename);
                CopyAll(info.Name, dir + "/");
            });
        }

        private static void CopyAll(String SourcePath, String DestinationPath)
        {
            String[] directories = Directory.GetDirectories(SourcePath, "*.*", SearchOption.AllDirectories);

            foreach (String dirPath in directories)
            {
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));
            }

            String[] files = Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories);

            foreach (String newPath in files)
            {
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath));
            }
        }

        private void type_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            // Kategorie herausfinden
            KeyValuePair<String, DiskType> c = MainWindow.Types.First(cat => cat.Value.Name == type.SelectedValue.ToString());
            file_1.IsEnabled = file_2.IsEnabled = c.Value.Command.Contains("{1}");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Main.addImageWindow = null;
        }
    }
}
