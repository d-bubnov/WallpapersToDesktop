using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WallpapersMoveToDesktop
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            SetIconToMainApplication();
        }

        private void SetIconToMainApplication()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = Properties.Resources.ResourceManager.GetObject("ApplicationMainIcon") as Icon;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += delegate(object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            listView.Items.Clear();

            var userProfile = System.Environment.GetEnvironmentVariable("USERPROFILE");

            if (userProfile != null)
            {
                var backupDir = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Desktop\Wallpapers"); 
                var sourceDir = string.Format(userProfile + @"\AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");

                DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
            
                foreach (var fileInfo in fileInfos)
                {
                    if (!Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }
                
                    try
                    {
                        var oldFileName = Path.Combine(sourceDir, fileInfo.Name);
                        var image = System.Drawing.Image.FromFile(oldFileName);

                        if (image.Height >= 1080 && image.Width >= 1920)
                        {
                            var newFileName = Path.Combine(backupDir, string.Format(fileInfo.Name + ".jpeg"));
                            if (File.Exists(newFileName))
                            {
                                continue;
                            }
                            File.Copy(oldFileName, newFileName);
                            listView.Items.Add(newFileName);
                        }
                    }
                    catch (Exception exception)
                    {
                        //MessageBox.Show(exception.Message);
                    }
                }
            }
            button.IsEnabled = true;
        }

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                var rotateTransform = btn.RenderTransform as RotateTransform;
                var transform = new RotateTransform(90 + (rotateTransform?.Angle ?? 0));
                btn.RenderTransform = transform;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Icon = null;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
