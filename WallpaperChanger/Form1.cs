using Guna.UI2.WinForms.Suite;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace WallpaperChanger
{
    public partial class Form1 : Form
    {
        public Random random { get; set; }
        public string[] _imagesLink { get; set; }
        public int imageCurrentIndex { get; set; }
        public Thread thread { get; set; }
        public Stopwatch Stopwatch { get; set; }
        public bool firstTime { get; set; } = true;
        public byte[] image { get; set; }
        public NotifyIcon notifyIcon { get; set; }
        public string[] imagesLink
        {
            get
            {
                return _imagesLink;
            }
            set
            {
                string[] temp = value;
                if (temp.Length > 0)
                {
                    _imagesLink = temp;
                    imageCurrentIndex = random.Next(temp.Length);
                    Stopwatch.Start();
                    string uid = Guid.NewGuid().ToString();
                    thread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        while (true)
                        {
                            try
                            {
                                if (firstTime || Stopwatch.Elapsed >= new TimeSpan(0, (int)guna2NumericUpDown1.Value, 0))
                                {
                                    string htmlContent = string.Empty;
                                    using (WebClient webClient = new WebClient())
                                    {
                                        htmlContent = webClient.DownloadString(imagesLink[imageCurrentIndex]);
                                    }
                                    if (String.IsNullOrEmpty(htmlContent)) { throw new Exception("empty response"); }
                                    HtmlDocument document = new HtmlDocument();
                                    document.LoadHtml(htmlContent);
                                    string downloadLink = document.DocumentNode.SelectSingleNode("//img[@itemprop='contentUrl']").GetAttributeValue("src", string.Empty).Trim();
                                    if (downloadLink.EndsWith(".jpg") || downloadLink.EndsWith(".png") || downloadLink.EndsWith(".jpeg"))
                                    {
                                        using (WebClient webClient = new WebClient())
                                        {
                                            image = webClient.DownloadData(downloadLink);
                                        }
                                        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(downloadLink));
                                        File.WriteAllBytes(tempPath, image);
                                        DesktopWallpaper.Change(tempPath);
                                        Thread.Sleep(500);
                                        File.Delete(tempPath);
                                        firstTime = false;
                                        Stopwatch.Restart();
                                    }
                                    imageCurrentIndex = random.Next(imagesLink.Length);
                                }
                                TimeSpan timeSpan = new TimeSpan(Stopwatch.Elapsed.Ticks - new TimeSpan(0, (int)guna2NumericUpDown1.Value, 0).Ticks);
                                this.Invoke(new MethodInvoker(() => label1.Text = $"Next Change: {Math.Abs(timeSpan.Minutes)}m : {Math.Abs(timeSpan.Seconds)}s"));
                                Thread.Sleep(500);
                            }
                            catch (System.Net.WebException ex) 
                            { 
                                this.Invoke(new MethodInvoker(() => label1.Text = "Server down.")); 
                                Thread.Sleep(30000); Stopwatch.Restart(); 
                                firstTime = true; 
                                continue; 
                            }
                        }
                    });
                    thread.Start();
                    this.Invoke(new MethodInvoker(() => label1.Text = "Total Images Found: " + (_imagesLink.Length).ToString()));
                }
            }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] config = Properties.Settings.Default.Config.Split('-');
            guna2TextBox1.Text = config[0];
            guna2NumericUpDown1.Value = Convert.ToInt32(config[1]);
            guna2NumericUpDown2.Value = Convert.ToInt32(config[2]);
            guna2NumericUpDown3.Value = Convert.ToInt32(config[3]);
          
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.Expect100Continue = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string config = guna2TextBox1.Text + "-" +
            guna2NumericUpDown1.Value + "-" +
            guna2NumericUpDown2.Value + "-" +
            guna2NumericUpDown3.Value;
            Properties.Settings.Default.Config = config;
            Properties.Settings.Default.Save();
        }


        private async void guna2Button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(guna2TextBox1.Text))
                {
                    throw new Exception("Search box is empty !");
                }
                guna2Button3.Enabled = false;
                label1.Text = "...";
                thread?.Abort();
                Stopwatch?.Stop();
                Stopwatch = new Stopwatch();
                random = new Random();
                firstTime = true;
                await Task.Run(() =>
                {
                    List<string> strings = new List<string>();
                    int min = Math.Min((int)guna2NumericUpDown2.Value, (int)guna2NumericUpDown3.Value);
                    int max = Math.Max((int)guna2NumericUpDown2.Value, (int)guna2NumericUpDown3.Value);
                    for (int i = min; i <= max; i++)
                    {
                        try
                        {
                            string htmlContent = string.Empty;
                            using (WebClient webClient = new WebClient())
                            {
                                htmlContent = new WebClient().DownloadString("https://www.wallpaperflare.com/search?wallpaper=" + guna2TextBox1.Text + "&page=" + i);
                            }
                            if (String.IsNullOrEmpty(htmlContent)) { MessageBox.Show("empty response"); return; }
                            HtmlDocument document = new HtmlDocument();
                            document.LoadHtml(htmlContent);
                            strings.AddRange(document.DocumentNode.SelectNodes("//a[@itemprop='url']").Select(x => x.GetAttributeValue("href", string.Empty).Trim() + "/download").ToArray());
                        }
                        catch { break; }
                    }
                    imagesLink = strings.ToArray();
                    if (!(imagesLink.Length > 0) || imagesLink == null)
                    {
                        throw new Exception("Error Reasons:\n" +
                            "[ - ] no images found on that page range. \n" +
                            "[ - ] wallpaperflare.com may down.\n" +
                            "[ - ] no internet connection.\n");
                    }
                });
                guna2Button3.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }


        private void guna2Button1_Click(object sender, EventArgs e)
        {
            try
            {
                notifyIcon = new NotifyIcon();
                notifyIcon.Icon = Properties.Resources.wallpaper;
                notifyIcon.Visible = true;
                notifyIcon.DoubleClick += (s, a) => { notifyIcon.Visible = false; this.Show(); };

                ContextMenu contextMenu = new ContextMenu();
                contextMenu.MenuItems.Add("Show", (s, a) => { notifyIcon.Visible = false; this.Show(); });
                contextMenu.MenuItems.Add("Exit", (s, a) => { notifyIcon.Visible = false; this.Close(); });

                notifyIcon.ContextMenu = contextMenu;

                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        Random fileRandom = new Random();
        private void label2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "Wallpaper" + fileRandom.Next(1000000, int.MaxValue) + ".jpg";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(saveFileDialog.FileName, image);
                MessageBox.Show("Wallpaper Saved:\n" + saveFileDialog.FileName, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        class DesktopWallpaper
        {
            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDWININICHANGE = 0x02;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

            public static void Change(string path)
            {
                if (string.IsNullOrEmpty(path)) return;
                SystemParametersInfo(SPI_SETDESKWALLPAPER,
                                     0,
                                     path,
                                     SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            Process.Start("https://wallpaperflare.com");
        }
    }
}
