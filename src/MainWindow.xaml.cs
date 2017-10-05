using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using System.Text;

namespace MediaLibrary
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Die Freigabe im Netzwerk, in der die DVD Images gespeichert werden.
        /// </summary>
        public static String NetworkPath { get; set; }

        /// <summary>
        /// Alle Disktypen, die genutzt werden können
        /// </summary>
        public static Dictionary<String, DiskType> Types { get; set; }

        /// <summary>
        /// Alle Kategorien die genutzt werden können
        /// </summary>
        public static List<String> Categories { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitMediaLibrary();
            Closing += delegate (Object sender, CancelEventArgs e) { e.Cancel = addImageWindow?.runner != null; addImageWindow?.Close(); };
        }

        /// <summary>
        /// Lädt die Mediathek und die dazugehörigen Einstellungen.
        /// </summary>
        public void InitMediaLibrary()
        {
            // Den Netzwerk-Pfad aus der Registry auslesen.
#if !DEBUG
            NetworkPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Mediathek", "NetworkPath", "") as String;
            if (String.IsNullOrWhiteSpace(NetworkPath))
            {
                throw new ArgumentNullException("NetworkPath");
            }
#else
            NetworkPath = "C:\\MediaLibrary\\";
#endif
            Types = new Dictionary<String, DiskType>();

            // Kategorien laden
            LoadCategories();

            ScanNetworkPath("Alle", "");
        }

        /// <summary>
        /// Lädt die Kategorien und Typen für DVDs aus der Netzwerk-Freigabe
        /// </summary>
        public void LoadCategories()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            foreach (String file in Directory.GetFiles(System.IO.Path.Combine(NetworkPath, "types"), "*.json"))
            {
                DiskType c = serializer.Deserialize<DiskType>(File.ReadAllText(file));
                Types.Add(System.IO.Path.GetFileNameWithoutExtension(file), c);
            }
            Categories = serializer.Deserialize<List<String>>(File.ReadAllText(System.IO.Path.Combine(NetworkPath, "categories.json")));
            List<String> temp = Categories.ToList();
            temp.Insert(0, "Alle");
            categories.ItemsSource = temp;
        }

        /// <summary>
        /// Lädt alle Images aus der Netzwerkfreigabe und updated die Oberfläche entsprechend.
        /// </summary>
        /// <param name="pattern"></param>
        public void ScanNetworkPath(String category, String pattern)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            grid.Children.Clear();
            Dictionary<String, MediaEntry> entries = new Dictionary<String, MediaEntry>();
            foreach (String dir in Directory.GetDirectories(NetworkPath))
            {
                if (dir.EndsWith("types")) continue;
                MediaEntry e = serializer.Deserialize<MediaEntry>(File.ReadAllText(System.IO.Path.Combine(dir, "entry.json")));
                if (pattern != "" && !Regex.IsMatch(e.Name, pattern, RegexOptions.IgnoreCase))
                {
                    continue;
                }
                if (category != "Alle" && e.Category != category)
                {
                    continue;
                }
                entries.Add(dir, e);
            }
            grid.Height = Math.Max((entries.Count * 57) + 10, 385);
            Int32[] margins = new Int32[] { 10, 10, 10, (Int32)grid.Height - 58 };
            foreach (KeyValuePair<String, MediaEntry> kvP in entries.OrderBy(e => e.Value.Name))
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
                    Content = "[" + e.Category + "] " + e.Name,
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
                    Types[e.Type].Execute(dir, e.File);
                };

                grid.Children.Add(play);
                margins[1] += 57;
                margins[3] -= 57;
            }
        }

        private void search_Click(Object sender, RoutedEventArgs e)
        {
            ScanNetworkPath(categories.SelectedValue.ToString(), searchbar.Text?.Trim() ?? "");
        }

        public AddImageWindow addImageWindow;

        private void add_Click(Object sender, RoutedEventArgs e)
        {
            if (addImageWindow == null)
            {
                addImageWindow = new AddImageWindow(this);
                addImageWindow.Show();
            }
            else
            {
                addImageWindow.Focus();
            }
        }

        private void categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScanNetworkPath(categories.SelectedValue.ToString(), "");
        }
    }

    public struct MediaEntry
    {
        public String Name { get; set; }
        public String Type { get; set; }
        public String Category { get; set; }
        public String File { get; set; }
    }

    public class DiskType
    {
        public String Name { get; set; }
        public String Command { get; set; }

        public void Execute(String dir, String file)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            CommandLineParser parser = new CommandLineParser();
            String[] args = parser.Parse((Command.Contains("{1}") ? String.Format(Command, dir, file) : String.Format(Command, dir))).ToArray();
            startInfo.FileName = args[0];
            startInfo.Arguments = String.Join(" ", args.Skip(1));
            process.StartInfo = startInfo;
            process.Start();
        }
    }

    public class CommandLineParser
    {
        char[] cmd; // source buffer
        StringBuilder buf; // output buffer
        int i; // current position within the source buffer

        public CommandLineParser()
        {
            cmd = null;
            buf = null;
            i = -1;
            return;
        }

        public IEnumerable<string> Parse(string commandLine)
        {
            cmd = commandLine.ToCharArray();
            buf = new StringBuilder();
            i = 0;

            while (i < cmd.Length)
            {
                char ch = cmd[i];

                if (char.IsWhiteSpace(ch)) { throw new InvalidOperationException(); }
                else if (ch == '\\') { ParseEscapeSequence(); }
                else if (ch == '"') { ParseQuotedWord(); }
                else { ParseBareWord(); }

                if (i >= cmd.Length || char.IsWhiteSpace(cmd[i]))
                {
                    string arg = buf.ToString();

                    yield return arg;

                    buf.Length = 0;
                    ConsumeWhitespace();
                }
            }
        }

        /// <summary>
        /// Parse a quoted word
        /// </summary>
        private void ParseQuotedWord()
        {

            // scan over the lead-in quotation mark w/o adding it to the buffer
            ++i;

            // scan the contents of the quoted word into the buffer
            while (i < cmd.Length && cmd[i] != '"')
            {
                char ch = cmd[i];
                if (ch == '\\') { ParseEscapeSequence(); }
                else { buf.Append(ch); ++i; }
            }

            // scan over the lead-out quotation mark w/o adding it to the buffer
            if (i < cmd.Length)
            {
                ++i;
            }
            return;
        }

        /// <summary>
        /// Parse a bareword
        /// </summary>
        private void ParseBareWord()
        {
            while (i < cmd.Length)
            {
                char ch = cmd[i];
                if (char.IsWhiteSpace(ch)) break; // whitespace terminates a bareword
                else if (ch == '"') break; // lead-in quote starts a quoted word
                else if (ch == '\\') break; // escape sequence terminates the bareword

                buf.Append(ch); // otherwise, keep reading this word                

                ++i;
            }
            return;
        }

        /// <summary>
        /// Parse an escape sequence of one or more backslashes followed an an optional trailing quotation mark
        /// </summary>
        private void ParseEscapeSequence()
        {
            //---------------------------------------------------------------------------------------------------------
            // The rule is that:
            //
            // * An even number of backslashes followed by a quotation mark ('"') means that
            //   - the backslashes are escaped, so half that many get injected into the buffer, and
            //   - the quotation mark is a lead-in/lead-out quotation mark that marks the start of a quoted word
            //     which does not get added to the buffer.
            //
            // * An odd number of backslashes followed by a quotation mark ('"') means that
            //   - the backslashes are escaped, so half that many get injected into the buffer, and
            //   - the quotation mark is escaped. It's a literal quotation mark that also gets injected into the buffer
            //
            // * Any number of backslashes that aren't followed by a quotation mark ('"') have no special meaning:
            //   all of them get added to the buffer as-sis.
            //
            //---------------------------------------------------------------------------------------------------------

            //
            // scan in the backslashes
            //
            int p = i; // start of the escape sequence
            while (i < cmd.Length && cmd[i] == '\\')
            {
                buf.Append('\\');
                ++i;
            }

            //
            // if the backslash sequence is followed by a quotation mark, it's an escape sequence
            //
            if (i < cmd.Length && cmd[i] == '"')
            {
                int n = (i - p); // find the number of backslashes seen
                int quotient = n >> 1; // n divide 2 ( 5 div 2 = 2 , 6 div 2 = 3 )
                int remainder = n & 1; // n modulo 2 ( 5 mod 2 = 1 , 6 mod 2 = 0 )

                buf.Length -= (quotient + remainder); // remove the unwanted backslashes

                if (remainder != 0)
                {
                    // the trailing quotation mark is an escaped, literal quotation mark
                    // add it to the buffer and increment the pointer
                    buf.Append('"');
                    ++i;
                }
            }
            return;
        }

        /// <summary>
        /// Consume inter-argument whitespace
        /// </summary>
        private void ConsumeWhitespace()
        {
            while (i < cmd.Length && char.IsWhiteSpace(cmd[i]))
            {
                ++i;
            }
            return;
        }
    }
}
