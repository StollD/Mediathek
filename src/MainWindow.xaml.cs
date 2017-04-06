using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaLibrary
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The path in the network
        /// </summary>
        public static String NetworkPath { get; set; }

        /// <summary>
        /// All the categories
        /// </summary>
        public static Dictionary<String, Category> Categories { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitMediaLibrary();
        }

        /// <summary>
        /// Loads the library and the settings
        /// </summary>
        public void InitMediaLibrary()
        {
            // Load the path from the registry
#if !DEBUG
            NetworkPath = Registry.LocalMachine.GetValue("SOFTWARE/HVH/MediaLibrary/NetworkPath", "") as String;
            if (String.IsNullOrWhiteSpace(NetworkPath))
            {
                throw new ArgumentNullException("NetworkPath");
            }
#else
            NetworkPath = "C:\\MediaLibrary\\";
#endif
            Categories = new Dictionary<String, Category>();

            // Load the categories
            LoadCategories();

            ScanNetworkPath("");
        }

        /// <summary>
        /// Loads the categories for DVDs from the disk
        /// </summary>
        public void LoadCategories()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            foreach (String file in Directory.GetFiles(System.IO.Path.Combine(NetworkPath, "categories"), "*.json"))
            {
                Category c = serializer.Deserialize<Category>(File.ReadAllText(file));
                Categories.Add(System.IO.Path.GetFileNameWithoutExtension(file), c);
            }
        }

        public void ScanNetworkPath(String pattern)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            grid.Children.Clear();
            Dictionary<String, MediaEntry> entries = new Dictionary<String, MediaEntry>();
            foreach (String dir in Directory.GetDirectories(NetworkPath))
            {
                if (dir.EndsWith("categories")) continue;
                MediaEntry e = serializer.Deserialize<MediaEntry>(File.ReadAllText(System.IO.Path.Combine(dir, "entry.json")));
                if (pattern != "" && !Regex.IsMatch(e.Name, pattern))
                    continue;
                entries.Add(dir, e);
            }
            grid.Height = Math.Max((entries.Count * 57) + 10, 385);
            Int32[] margins = new Int32[] { 10, 10, 10, (Int32)grid.Height - 58 };
            foreach (var kvP in entries.OrderBy(e => e.Value.Name))
            {
                MediaEntry e = kvP.Value;
                String dir = kvP.Key;
                grid.Children.Add(new Rectangle()
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new Double[] { 1, 4 }),
                    SnapsToDevicePixels = true,
                    Margin = new Thickness(margins[0], margins[1], margins[2], margins[3]),
                    RadiusX = 2,
                    RadiusY = 2
                });
                grid.Children.Add(new Label()
                {
                    Content = e.Name,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(18, margins[1] + 5, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Calibri"),
                    FontSize = 20
                });
                Button play = new Button()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(331, margins[1] + 10, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top, 
                    Width = 119,
                    Height = 28,
                    Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221)),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 29, 139, 44)),
                    Content = new Grid()
                    {
                        Height = 28,
                        Width = 92,
                    }
                };
                (play.Content as Grid).Children.Add(new Label()
                {
                    Content = "Starten",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground = new SolidColorBrush(Colors.Black),
                    Height = 31,
                    Width = 88,
                    Margin = new Thickness(0, -3, 0, 0),
                    FontFamily = new FontFamily("Calibri"),
                    FontSize = 16
                });
                (play.Content as Grid).Children.Add(new PackIconFontAwesome()
                {
                    Kind = PackIconFontAwesomeKind.Play,
                    Margin = new Thickness(62, 2, 0, 0),
                    Height = 20,
                    Width = 26
                });

                // Events
                play.Click += delegate
                {
                    // Execute the stored command
                    Categories[e.Category].Execute(dir, e.File);
                };

                grid.Children.Add(play);
                margins[1] += 57;
                margins[3] -= 57;
            }
        }

        private void search_Click(Object sender, RoutedEventArgs e)
        {
            ScanNetworkPath(searchbar.Text ?? "");
        }
    }

    public struct MediaEntry
    {
        public String Name { get; set; }
        public String Category { get; set; }
        public String File { get; set; }
    }

    public class Category
    {
        public String Name { get; set; }
        public String Command { get; set; }

        public void Execute(String dir, String file)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + (Command.Contains("{1}") ? String.Format(Command, dir, file) : String.Format(Command, dir));
            process.StartInfo = startInfo;
            process.Start();
        }
    }
}
