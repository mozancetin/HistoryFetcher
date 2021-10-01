using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace HistoryFetcher
{
    public partial class Menu : Form
    {
        List<Item> allItems = new List<Item>();
        string dir = "";
        DateTime From = new DateTime();
        DateTime To = new DateTime();
        int TitleLength = 150;
        int UrlLength = 125;

        public Menu()
        {
            InitializeComponent();
            ItemBox.View = View.Details;
            ItemBox.GridLines = true;
            ItemBox.CheckBoxes = false;
            ItemBox.FullRowSelect = true;
            ItemBox.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            ItemBox.Columns[2].Width = 180;
            ItemBox.Refresh();
        }

        private void CreateListViewItem(ListView view, Item obj)
        {
            ListViewItem item = new ListViewItem(" " + obj.title);
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, obj.url));
            item.SubItems.Add(new ListViewItem.ListViewSubItem(item, obj.date.ToString()));
            if (obj.title.Length > TitleLength) { TitleLength = obj.title.Length; }
            if (obj.url.Length > UrlLength) { UrlLength = obj.url.Length; }
            item.Tag = obj;
            view.Items.Add(item);
        }

        private void ReloadList()
        {
            ItemBox.Items.Clear();
            allItems.ForEach(i => { CreateListViewItem(ItemBox, i); });
            if (TitleLength * 3 <= 1250)
                ItemBox.Columns[0].Width = TitleLength * 3;
            else
                ItemBox.Columns[0].Width = 750;
            if (UrlLength * 3 <= 1500)
                ItemBox.Columns[1].Width = UrlLength * 3;
            else
                ItemBox.Columns[1].Width = 1500;
            ItemBox.Refresh();
        }

        private DateTime DateFromWebkit(string timestamp)
        {
            return new DateTime(1601, 1, 1).AddMilliseconds(double.Parse(timestamp) / 1000);
        }

        private bool Control(string title, string url) 
        {
            if (titleSearchBox.Items.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(title)) { return false; }
                foreach(string word in titleSearchBox.Items)
                {
                    if (title.ToLower().Contains(word.ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (urlSearchBox.Items.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(url)) { return false; }
                foreach (string word in urlSearchBox.Items)
                {
                    if (url.ToLower().Contains(word.ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (banTitleBox.Items.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(title)) { return false; }
                foreach (string word in banTitleBox.Items)
                {
                    if (title.ToLower().Contains(word.ToLower()))
                    {
                        return false;
                    }
                }
            }

            if (banUrlBox.Items.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(url)) { return false; }
                foreach (string word in banUrlBox.Items)
                {
                    if (url.ToLower().Contains(word.ToLower()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dateTimePicker1.Value.Date > dateTimePicker2.Value.Date)
            {
                MessageBox.Show("The start date cannot be greater than the due date.");
                dateTimePicker2.Value = dateTimePicker1.Value;
                return;
            }

            foreach(Process p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("chrome"))
                {
                    MessageBox.Show("In order to use the program, you must first close Google Chrome.");
                    return;
                }
            }

            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dir = userPath + "\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\";

            if (!Directory.Exists(dir)) {
                MessageBox.Show("I couldn't find the path.");
                return; 
            }
            else
            {
                From = dateTimePicker1.Value.Date;
                To = dateTimePicker2.Value.Date;
                ItemBox.Columns[0].Width = 300;
                ItemBox.Columns[1].Width = 300;
                ItemBox.Items.Clear();
                CreateListViewItem(ItemBox, new Item() { title = "Please Wait...", url = "Please Wait..." });
                ItemBox.Refresh();
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            allItems.Clear();
            
            if (!string.IsNullOrWhiteSpace(dir))
            {
                using (var connection = new SqliteConnection($"Data Source={dir + "History"}"))
                {
                    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_dynamic_cdecl());
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT url, last_visit_time, title FROM urls";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string lastVisit = reader.GetString(1);
                            DateTime lV = DateFromWebkit(lastVisit).AddHours(3);
                            if (lV >= From && lV <= To.AddHours(23).AddMinutes(59).AddSeconds(59))
                            {
                                string url = reader.GetString(0);
                                string title = reader.GetString(2);
                                if (Control(title, url))
                                    allItems.Add(new Item() { title = title, url = url, date = lV });
                            }
                        }
                    }
                }
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            allItems = allItems.OrderBy(i => i.date).Reverse().ToList();
            ReloadList();
            button9.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            titleSearchBox.Items.Add(searchBox.Text);
            searchBox.Text = string.Empty;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (titleSearchBox.SelectedItem != null)
            {
                titleSearchBox.Items.Remove(titleSearchBox.SelectedItem);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            urlSearchBox.Items.Add(textBox1.Text.Split(' ')[0]);
            textBox1.Text = string.Empty;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (urlSearchBox.SelectedItem != null)
            {
                urlSearchBox.Items.Remove(urlSearchBox.SelectedItem);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            banTitleBox.Items.Add(textBox3.Text);
            textBox3.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (banTitleBox.SelectedItem != null)
            {
                banTitleBox.Items.Remove(banTitleBox.SelectedItem);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            banUrlBox.Items.Add(textBox2.Text.Split(' ')[0]);
            textBox2.Text = string.Empty;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (banUrlBox.SelectedItem != null)
            {
                banUrlBox.Items.Remove(banUrlBox.SelectedItem);
            }
        }

        private void ItemBox_ItemActivate(object sender, EventArgs e)
        {
            if (ItemBox.SelectedItems.Count > 0)
            {
                Item item = (Item)ItemBox.SelectedItems[0].Tag;
                DetailsWindow dW = new DetailsWindow(item.title, item.url);
                this.Hide();
                dW.ShowDialog();
                this.Show();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (ItemBox.Items.Count <= 0) { return; }
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text File (*.txt)|*.txt|JSON File (*.json)|*.json";
            saveFileDialog1.Title = "Select Save Path";
            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                string path = saveFileDialog1.FileName;
                string ext = path.Split('\\').Last<string>().Split('.').Last<string>();
                if (ext == "txt")
                {
                    foreach(ListViewItem i in ItemBox.Items)
                    {
                        Item item = (Item)i.Tag;
                        File.AppendAllText(path, item.title + "\t Link: " + item.url + "\n");
                    }
                }
                else if (ext == "json")
                {
                    JsonSerializerOptions options = new JsonSerializerOptions() {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    };
                    List<Item> allItems = new List<Item>();
                    foreach(ListViewItem item in ItemBox.Items)
                    {
                        allItems.Add((Item)item.Tag);
                    }

                    string jsonString = JsonSerializer.Serialize(allItems, options);
                    File.WriteAllText(path, jsonString);
                }

                MessageBox.Show("Successfully saved.");
            }
        }
    }
}
