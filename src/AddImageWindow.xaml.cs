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
            Closing += delegate (Object sender, CancelEventArgs e) { e.Cancel = runner != null; };

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
                category.IsEnabled = false;
                file_1.IsEnabled = false;
                file_2.IsEnabled = false;
                add.IsEnabled = false;
                progress.IsEnabled = false;
            }

            // Kategorien erfassen
            List<String> category_values = new List<String>();
            foreach (Category c in MainWindow.Categories.Values)
            {
                category_values.Add(c.Name);
            }
            if (category_values.Any())
            {
                category.ItemsSource = category_values;
            }
            else
            {
                sources.IsEnabled = false;
                title.IsEnabled = false;
                category.IsEnabled = false;
                file_1.IsEnabled = false;
                file_2.IsEnabled = false;
                add.IsEnabled = false;
                progress.IsEnabled = false;
            }
        }

        private Task runner;

        /// <summary>
        /// Die DVD hinzufügen.
        /// </summary>
        private async void add_Click(Object sender, RoutedEventArgs e)
        {
            String volumename = sources.Text.Split('[')[0].Trim();
            progress.IsEnabled = true;
            progress.IsIndeterminate = true;
            sources.IsEnabled = false;
            title.IsEnabled = false;
            category.IsEnabled = false;
            file_1.IsEnabled = false;
            file_2.IsEnabled = false;
            add.IsEnabled = false;
            runner = AddDVD(volumename, title.Text, category.SelectedValue.ToString(), file_1.Text);
            await runner;
            runner = null;
            Close();
            Main.ScanNetworkPath(Main.searchbar.Text?.Trim() ?? "");
        }

        private async Task AddDVD(String volumename, String dvdtitle, String category, String file)
        {
            await Task.Run(() =>
            {
                Guid ID = Guid.NewGuid(); // Eindeutige ID für den Ordner
                String dir = Path.Combine(MainWindow.NetworkPath, ID.ToString());
                Directory.CreateDirectory(dir);

                // Kategorie herausfinden
                KeyValuePair<String, Category> c = MainWindow.Categories.First(cat => cat.Value.Name == category);

                // Entry Datei schreiben
                File.WriteAllText(Path.Combine(dir, "entry.json"),
                    "{\n    \"Name\": \"" + dvdtitle + "\",\n    \"Category\": \"" + c.Key + "\",\n    \"File\": \"" + (c.Value.Command.Contains("{1}") ? file : "") + "\"\n}");

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

        private void category_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            // Kategorie herausfinden
            KeyValuePair<String, Category> c = MainWindow.Categories.First(cat => cat.Value.Name == category.SelectedValue.ToString());
            file_1.IsEnabled = file_2.IsEnabled = c.Value.Command.Contains("{1}");
        }
    }
}
