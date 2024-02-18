using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PopLanguageEditor
{
    public partial class MainWindow : Window
    {
        static public int VersionMajor = 1;
        static public int VersionMinor = 0;
        static public int VersionPatch = 0;
        private string EditorName = "PopLanguageEditor v" + VersionMajor + "." + VersionMinor + "." + VersionPatch;
        private const string IDS_FILE = "ids.txt";
        private const string TEXT_FILE = "text.txt";

        public class LangEntry
        {
            public int Line { get; set; }
            public int ID { get; set; }
            public string Text { get; set; }
            public string Original { get; set; }
        }

        public class LangCache
        {
            public int ID { get; set; }
            public string Text { get; set; }
        }

        private string FileLoaded = "";
        private List<LangEntry> Language;


        private void SaveCfg()
        {
            string filename = "cfg";
            if (File.Exists(filename))
                File.Delete(filename);
            using StreamWriter file = new StreamWriter(filename);
            file.WriteLine(FileLoaded);
        }

        private void LoadCfg()
        {
            string filename = "cfg";
            if (File.Exists(filename))
            {
                string[] lines = File.ReadAllLines(filename);
                if (lines.Length > 0)
                {
                    FileLoaded = lines[0];
                }
            }
        }

        void CreateCache(ref List<string> lines)
        {
            List<LangCache> data = new List<LangCache>();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                short n = -1;
                if (line[0] == '-' && line[line.Length - 1] == '-')
                {
                    string str = lines[i].Substring(1, line.Length - 2);
                    Int16.TryParse(str, out n);
                }

                data.Add(new LangCache { ID = n, Text = line });
            }

            if (File.Exists(IDS_FILE))
                File.Delete(IDS_FILE);
            if (File.Exists(TEXT_FILE))
                File.Delete(TEXT_FILE);

            using StreamWriter file = new StreamWriter(IDS_FILE);
            foreach (var entry in data)
                file.WriteLine(entry.ID);

            using StreamWriter file2 = new StreamWriter(TEXT_FILE);
            foreach (var entry in data)
                file2.WriteLine(entry.Text);

            file.Close();
            file2.Close();
        }

        List<LangCache> LoadCache()
        {
            List<LangCache> data = new List<LangCache>();
            string[] lines = File.ReadAllLines(IDS_FILE);
            string[] lines2 = File.ReadAllLines(TEXT_FILE);
            for (int i = 0; i < lines.Length; i++)
            {
                data.Add(new LangCache { ID=Int16.Parse(lines[i]), Text=lines2[i] });
            }
            return data;
        }

        void LoadLanguageFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            int start = 0, end = 0;
            List<LangEntry> data = new List<LangEntry>();
            string fileName = filePath;

            List<string> lines = new List<string>();
            byte[] buffer = File.ReadAllBytes(fileName);
            for (int i = 0; i < buffer.Length; i++) {
                if (buffer[i] == 00 && buffer[i+1] == 00 && buffer[i+2] == 00)
                {
                    byte[] b = new byte[i - start + 1];
                    Buffer.BlockCopy(buffer, start, b, 0, b.Length);
                    lines.Add(Encoding.Unicode.GetString(b));
                    i += 3;
                    start = i;
                }
            }

            List<LangCache> cache = LoadCache();
            for (int i = 0; i < lines.Count; i++)
            {
                // Langauge files can contain additional entries beyond the expected size (e.g. beta version),
                // while LangCache was generated based on the original English lang file.
                // We need this check to avoid OOB crashes. 
                if (i >= cache.Count)
                    data.Add(new LangEntry { Line=i, ID=-1, Text=lines[i], Original="" });
                else
                    data.Add(new LangEntry { Line=i, ID=cache[i].ID, Text=lines[i], Original=cache[i].Text });
            }

            //CreateCache(ref lines);
            grid.ItemsSource = data;
            FileLoaded = filePath;
            Language = data;
            this.Title = EditorName + " - " + filePath;
            SaveCfg();
        }

        private void menu_New(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset the language file back to default? This will erase any custom strings you may have modified.", EditorName, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                List<LangEntry> data = new List<LangEntry>();
                List<LangCache> cache = LoadCache();
                for (int i = 0; i < cache.Count; i++)
                {
                    data.Add(new LangEntry { Line = i, ID = cache[i].ID, Text = cache[i].Text, Original = cache[i].Text });
                }
                grid.ItemsSource = data;
            }
        }

        private void OpenLangauge()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
                LoadLanguageFile(dlg.FileName);
        }

        private void menu_Open(object sender, RoutedEventArgs e)
        {
            OpenLangauge();
        }

        private void SaveLanguage()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Data File (*.dat)|*.dat";
            dlg.DefaultExt = "dat";
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == true)
            {
                byte[] b = new byte[2] { 0x0, 0x0 };
                string filename = dlg.FileName;
                if (File.Exists(filename)) File.Delete(filename);
                Encoding encoding = new UnicodeEncoding(false, false);
                using FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write);
                foreach (LangEntry entry in Language)
                {
                    file.Write(Encoding.Unicode.GetBytes(entry.Text));
                    file.Write(b, 0, b.Length);
                }
                file.Close();
            }
        }

        private void menu_Save(object sender, RoutedEventArgs e)
        {
            SaveLanguage();
        }

        private void menu_Export(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text File (*.txt)|*.txt";
            dlg.DefaultExt = "txt";
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;
                if (File.Exists(filename)) File.Delete(filename);
                using StreamWriter file = new StreamWriter(filename);
                foreach (LangEntry entry in Language)
                {
                    file.WriteLine(entry.Text);
                }
                file.Close();
            }
        }

        private void menu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Title = EditorName;
            if (!File.Exists(IDS_FILE))  {
                MessageBox.Show("Missing file: " + IDS_FILE, EditorName, MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(1);
            }
            if (!File.Exists(TEXT_FILE)) {
                MessageBox.Show("Missing file: " + TEXT_FILE, EditorName, MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(1);
            }
            LoadCfg();
            LoadLanguageFile(FileLoaded);
        }

        private void FilterByLine()
        {
            List<LangEntry> filtered = new List<LangEntry>();
            short n = -1;
            if (Int16.TryParse(searchBox.Text, out n))
            {
                foreach (var entry in Language)
                {
                    if (entry.Line == n)
                    {
                        filtered.Add(entry);
                    }
                }
            }

            grid.ItemsSource = filtered;
            return;
        }

        private void FilterByID()
        {
            List<LangEntry> filtered = new List<LangEntry>();
            short n = -1;
            if (Int16.TryParse(searchBox.Text, out n))
            {
                foreach (var entry in Language)
                {
                    if (entry.ID == n)
                    {
                        filtered.Add(entry);
                    }
                }
            }

            grid.ItemsSource = filtered;
            return;
        }

        private void FilterByText()
        {
            List<LangEntry> filtered = new List<LangEntry>();

            if ((bool)!searchMatchCase.IsChecked)
            {
                foreach (var entry in Language)
                {
                    if (entry.Text.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.Add(entry);
                    }
                }
            }
            else
            {
                foreach (var entry in Language)
                {
                    string[] words = entry.Text.Split(' ');
                    foreach (string word in words)
                    {
                        if (word.ToLower() == searchBox.Text.ToLower())
                            filtered.Add(entry);
                    }
                }
            }

            grid.ItemsSource = filtered;
            return;
        }

        private void FilterByOriginal()
        {
            List<LangEntry> filtered = new List<LangEntry>();

            if ((bool)!searchMatchCase.IsChecked)
            {
                foreach (var entry in Language)
                {
                    if (entry.Original.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.Add(entry);
                    }
                }
            }
            else
            {
                foreach (var entry in Language)
                {
                    string[] words = entry.Original.Split(' ');
                    foreach (string word in words)
                    {
                        if (word.ToLower() == searchBox.Text.ToLower())
                            filtered.Add(entry);
                    }
                }
            }

            grid.ItemsSource = filtered;
            return;
        }

        private void FilterOnlyIDs()
        {
            List<LangEntry> filtered = new List<LangEntry>();
            foreach (var entry in Language)
            {
                if (entry.ID != -1)
                {
                    filtered.Add(entry);
                }
            }
            
            grid.ItemsSource = filtered;
            return;
        }


        private void FilterEntries()
        {
            if ((bool)searchNone.IsChecked)
            {
                grid.ItemsSource = Language;
                return;
            }

            if ((bool)searchLine.IsChecked)
            {
                FilterByLine();
                return;
            }

            if ((bool)searchByID.IsChecked)
            {
                FilterByID();
                return;
            }

            if ((bool)searchByText.IsChecked)
            {
                FilterByText();
                return;
            }

            if ((bool)searchByOriginal.IsChecked)
            {
                FilterByOriginal();
                return;
            }
        }

        private void searchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterEntries();
        }

        private void searchNone_Click(object sender, RoutedEventArgs e)
        {
            FilterEntries();
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenLangauge();
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveLanguage();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Copyright © 2022-2024 Toksisitee (https://github.com/Toksisitee)" +
                Environment.NewLine + Environment.NewLine +
                "PopLanguageEditor is free software: you can redistribute it and / or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. PopLanguageEditor is distributed in the hope that it will be useful,but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.",
                EditorName, MessageBoxButton.OK, MessageBoxImage.Information); 
        }
    }
}
