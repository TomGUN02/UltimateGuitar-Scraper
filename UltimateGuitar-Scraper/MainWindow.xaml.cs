using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ModernWpf;

namespace UltimateGuitar_Scraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetTheme(isDarkTheme);

        }

        DataTable table = new DataTable();
        private bool isDarkTheme = Properties.Settings.Default.Theme; // Track the current theme

        public void SearchUrl(string query, string tabType, int page)
        {
            // Save the search text & tab type
            Properties.Settings.Default.TabSearchInput = query;
            Properties.Settings.Default.TabType = tabType;
            Properties.Settings.Default.Save();
            // New html & webclient
            var doc = new HtmlDocument();
            var web = new WebClient();
            // Generate the Url with the selected tab type & page (default page is 1)
            string url = null;
            switch (tabType)
            {
                case "Tabs":
                    {
                        url = @"https://www.ultimate-guitar.com/search.php?search_type=title&value=" +
                              query + "&type=200" + "&page=" + page;
                        break;
                    }
                case "Chords":
                    {
                        url = @"https://www.ultimate-guitar.com/search.php?search_type=title&value=" +
                              query + "&type=300" + "&page=" + page;
                        break;
                    }
                case "Pro":
                    {
                        url = @"https://www.ultimate-guitar.com/search.php?search_type=title&value=" +
                              query + "&type=500" + "&page=" + page;
                        break;
                    }
            }

            try
            {
                // Download from the Generated url
                string source_code = web.DownloadString(url);
                doc.LoadHtml(source_code);
                // root to find the JSon code
                var nodes = doc.DocumentNode.SelectNodes("//div[@class='js-store']");
                // Clear the Datatable and Columns before to make sure
                table.Clear();
                table.Columns.Clear();
                // Checks the html file for Div for the root node
                foreach (var node in nodes)
                {
                    // This cleans the code before parsing
                    string jsonstring = node.Attributes["data-content"].DeEntitizeValue;
                    JObject jObject = JObject.Parse(jsonstring);

                    // Start generating table
                    // Add Colums to the DataTable
                    table.Columns.Add("Artist");
                    table.Columns.Add("Song Name", typeof(string));
                    table.Columns.Add("Part", typeof(string));
                    table.Columns.Add("Version", typeof(string));
                    table.Columns.Add("Votes", typeof(double));
                    table.Columns.Add("Rating", typeof(string));
                    table.Columns.Add("Download", typeof(string));
                    table.Columns.Add("Url", typeof(string));
                    // Put pages number on the UI
                    PageCurrentBox.Text = jObject.SelectToken("store.page.data.pagination.current").Value<string>();
                    PageTotalBox.Text = jObject.SelectToken("store.page.data.pagination.total").Value<string>();
                    // Enable Next Page if 2 pages+
                    int tot = Convert.ToInt16(PageTotalBox.Text);
                    int cp = Convert.ToInt16(PageCurrentBox.Text);
                    if ((tot > 1) && (tot != cp))
                    {
                        PageNext.IsEnabled = true;
                        PagePrevious.IsEnabled = false;
                    }

                    if ((tot > 1) && (cp < tot) && (tot != cp) && (tot > cp) && (cp != 1))
                    {
                        PageNext.IsEnabled = true;
                        PagePrevious.IsEnabled = true;
                    }
                    // Disable Previous button

                    if ((cp <= tot) && (cp != 1) && (tot == cp))
                    {
                        PageNext.IsEnabled = false;
                        PagePrevious.IsEnabled = true;
                    }

                    if (tot == 1)
                    {
                        PageNext.IsEnabled = false;
                        PagePrevious.IsEnabled = false;
                    }

                    int y = 0;
                    foreach (var a in jObject.SelectTokens("store.page.data.results[*]"))
                    {
                        var ab = "store.page.data.results[" + y + "]";
                        var ba = jObject.SelectToken(ab);
                        int z = 0;
                        if (ba.SelectToken("id") != null)
                        {
                            // Add Data Rows to the DataTable
                            if (tabType == "Pro")
                            {
                                object[] o =
                                {
                                    ba.SelectToken("artist_name").ToString(),
                                    ba.SelectToken("song_name").ToString(),
                                    ba.SelectToken("part").ToString(),
                                    ba.SelectToken("version").ToString(),
                                    ba.SelectToken("votes").ToString(),
                                    Math.Round((double) ba.SelectToken("rating"), 1),
                                    "Download",
                                    ba.SelectToken("tab_url").ToString()
                                };

                                table.Rows.Add(o);
                            }
                            else
                            {
                                object[] o =
                                {
                                    ba.SelectToken("artist_name").ToString(),
                                    ba.SelectToken("song_name").ToString(),
                                    ba.SelectToken("part").ToString(),
                                    ba.SelectToken("version").ToString(),
                                    ba.SelectToken("votes").ToString(),
                                    Math.Round((double) ba.SelectToken("rating"), 1),
                                    "View",
                                    ba.SelectToken("tab_url").ToString()
                                };

                                table.Rows.Add(o);
                            }
                            // Change Download rows into Buttons
                            TabList.AutoGeneratingColumn += (s, e) =>
                            {
                                if (e.PropertyName == "Download")
                                {
                                    DataGridTemplateColumn buttonColumn = new DataGridTemplateColumn();
                                    DataTemplate buttonTemplate = new DataTemplate();
                                    FrameworkElementFactory buttonFactory = new FrameworkElementFactory(typeof(Button));
                                    buttonTemplate.VisualTree = buttonFactory;
                                    //add handler or you can add binding to command if you want to handle click
                                    buttonFactory.AddHandler(Button.ClickEvent,
                                        new RoutedEventHandler(ButtonView_Click));
                                    buttonFactory.SetBinding(Button.ContentProperty, new Binding("Download"));
                                    buttonColumn.CellTemplate = buttonTemplate;
                                    e.Column = buttonColumn;
                                }
                            };


                            z++;
                        }

                        y++;
                    }
                }

                TabList.DataContext = table.DefaultView;
                TabList.Columns[0].Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                TabList.Columns[1].Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                TabList.Columns[2].Width = 80;
                TabList.Columns[3].Width = 66;
                TabList.Columns[4].Width = 62;
                TabList.Columns[5].Width = 60;
                TabList.Columns[6].Width = 50;
                TabList.Columns[7].Visibility = Visibility.Hidden;

                var border = VisualTreeHelper.GetChild(TabList, 0) as Decorator;
                if (border != null)
                {
                    var scrollViewer = border.Child as ScrollViewer;
                    scrollViewer.ScrollToTop();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse resp = ex.Response as HttpWebResponse;
                    if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        System.Windows.MessageBox.Show("Error 404. Tab not found.");
                        table.Clear();
                        table.Columns.Clear();
                    }
                    else
                        System.Windows.MessageBox.Show("Error");

                    //throw;
                }
                else
                    System.Windows.MessageBox.Show("Error");

                //throw;
            }
        }

        public void PrintTab(string url, bool pro)
        {
            var doc2 = new HtmlDocument();
            var web2 = new WebClient();
            if (pro == false)
            {
                try
                {
                    string source_code = web2.DownloadString(url);
                    doc2.LoadHtml(source_code);
                    foreach (var node2 in doc2.DocumentNode.SelectNodes("//div[@class='js-store']"))
                    {
                        string jsonstring2 = node2.Attributes["data-content"].DeEntitizeValue;
                        JObject jObject2 = JObject.Parse(jsonstring2);
                        Console.WriteLine(jObject2.ToString());
                        var window = new TabWindow(jObject2.SelectToken("store.page.data.tab_view.wiki_tab.content")
                                .Value<string>().Replace("[tab]", "").Replace("[/tab]", "").Replace("[ch]", "")
                                .Replace("[/ch]", ""))
                        { Owner = this };
                        window.ShowDialog();
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        HttpWebResponse resp = ex.Response as HttpWebResponse;
                        if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                        {
                            System.Windows.MessageBox.Show("Error");
                        }
                        else
                            System.Windows.MessageBox.Show("Error");
                        //throw;
                    }
                    else
                        System.Windows.MessageBox.Show("Error");
                    //throw;
                }
            }
            else
            {
                Regex rx = new Regex(@"pro-(.*?)=");
                string downloadUrl = "https://tabs.ultimate-guitar.com/tab/download?id=" + rx.Match(url + "=").Groups[1].Value + "&session_id=";

                string tempPath = Path.GetTempPath();

                WebClient webClient = new WebClient();
                webClient.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
                webClient.Headers.Add("Sec-Fetch-User", "?1");
                webClient.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                webClient.Headers.Add("referrer", "https://tabs.ultimate-guitar.com/tab/jimi-hendrix/little-wing-guitar-pro-237153");
                webClient.Headers.Add("method", "GET");

                var responseStream = new GZipStream(webClient.OpenRead(downloadUrl), CompressionMode.Decompress);
                var reader = new StreamReader(responseStream);
                var textResponse = reader.ReadToEnd();
                Console.WriteLine(textResponse);
                /*var task = DownloadFileAsync(downloadUrl);
                task.Wait(1000);*/

                /*var dialog = new SaveFileDialog();
                dialog.Filter = "Guitar Pro (*.gp;*.gp3;*.gp4;*.gp5;*.gpx)|*.gp;*.gp3;*.gp4;*.gp5;*.gpx";
                if (dialog.ShowDialog() == true)
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(url, dialog.FileName);
                    }
                }*/
            }
        }
        static async Task DownloadFileAsync(string url)
        {
            HttpClient client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
            HttpResponseMessage response = await client.GetAsync(url);
            Console.WriteLine(response.ToString());
            // Get the file name from the content-disposition header.
            // This is nasty because of bug in .net: http://stackoverflow.com/questions/21008499/httpresponsemessage-content-headers-contentdisposition-is-null
            string fileName = response.Content.Headers.GetValues("Content-Disposition")
                .Select(h => Regex.Match(h, @"(?<=filename=).+$").Value)
                .FirstOrDefault()
                .Replace('/', '_');
            using (FileStream file = File.Create(fileName))
            {
                await response.Content.CopyToAsync(file);
            }
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            int rowNumber = TabList.SelectedIndex;
            // is pro tab? =2
            if (TabTypeCB.SelectedIndex == 2)
            {
                PrintTab(table.Rows[rowNumber]
                    [7].ToString(), true);
            }
            else
            {
                PrintTab(table.Rows[rowNumber]
                    [7].ToString(), false);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            switch (TabTypeCB.SelectedIndex)
            {
                case 0:
                    {
                        SearchUrl(TabQuery.Text, "Tabs", 1);
                        break;
                    }
                case 1:
                    {
                        SearchUrl(TabQuery.Text, "Chords", 1);
                        break;
                    }
                case 2:
                    {
                        SearchUrl(TabQuery.Text, "Pro", 1);
                        break;
                    }
            }
        }

        private void PageNext_Click(object sender, RoutedEventArgs e)
        {
            int page = Convert.ToInt16(PageCurrentBox.Text) + 1;
            SearchUrl(Properties.Settings.Default.TabSearchInput, Properties.Settings.Default.TabType, page);
        }

        private void PagePrevious_Click(object sender, RoutedEventArgs e)
        {
            int page = Convert.ToInt16(PageCurrentBox.Text) - 1;
            SearchUrl(Properties.Settings.Default.TabSearchInput, Properties.Settings.Default.TabType, page);
        }

        private void TabQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDarkTheme)
            {
                isDarkTheme = !isDarkTheme;
                Properties.Settings.Default.Theme = false;
                SetTheme(isDarkTheme);
            }
            else
            {
                isDarkTheme = true;
                Properties.Settings.Default.Theme = true;
                SetTheme(isDarkTheme);

            }
            Properties.Settings.Default.Save();
        }

        private void SetTheme(bool darkTheme)
        {
            if (darkTheme)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                BitmapImage bitmageImage = new BitmapImage(new Uri("pack://application:,,,/UltimateGuitar-Scraper;component/png/dark.png"));
                ToggleThemeButtonImage.Source = bitmageImage;

            }
            else
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                BitmapImage bitmageImage = new BitmapImage(new Uri("pack://application:,,,/UltimateGuitar-Scraper;component/png/light.png"));
                ToggleThemeButtonImage.Source = bitmageImage;

            }
        }
    }
}