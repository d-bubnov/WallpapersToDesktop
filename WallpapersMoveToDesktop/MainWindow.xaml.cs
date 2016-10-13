using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WallpapersMoveToDesktop
{
    public partial class MainWindow : Window
    {
        #region Fields and Properties

        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private List<string> _existsImages = new List<string>();
        private readonly string _backupDir = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Wallpapers");

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            SetIconToMainApplication();
            LoadExistsImages();
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

        private void LoadExistsImages()
        {
            _existsImages.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(_backupDir);
            FileInfo[] fileInfos = directoryInfo.GetFiles();

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    var fileName = Path.Combine(_backupDir, fileInfo.Name);
                    var image = System.Drawing.Image.FromFile(fileName);

                    using (var memoryStream = new MemoryStream())
                    {
                        image.Save(memoryStream, image.RawFormat);
                        _existsImages.Add(Convert.ToBase64String(memoryStream.ToArray()));
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void AddToListExistImages(System.Drawing.Image image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, image.RawFormat);
                _existsImages.Add(Convert.ToBase64String(memoryStream.ToArray()));
            }
        }

        private bool ContainsImage(System.Drawing.Image originalImage)
        {
            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    originalImage.Save(memoryStream, originalImage.RawFormat);
                    var stringImage = Convert.ToBase64String(memoryStream.ToArray());
                    if (!_existsImages.Contains(stringImage))
                        return false;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return true;
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
                var sourceDir = string.Format(userProfile + @"\AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");
                DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
            
                foreach (var fileInfo in fileInfos)
                {
                    if (!Directory.Exists(_backupDir))
                    {
                        Directory.CreateDirectory(_backupDir);
                    }
                
                    try
                    {
                        var oldFileName = Path.Combine(sourceDir, fileInfo.Name);
                        var image = System.Drawing.Image.FromFile(oldFileName);

                        if (image.Height >= 1080 && image.Width >= 1920)
                        {
                            var newFileName = Path.Combine(_backupDir, string.Format(fileInfo.Name + ".jpeg"));
                            if (File.Exists(newFileName) || ContainsImage(image))
                            {
                                continue;
                            }
                            File.Copy(oldFileName, newFileName);
                            AddToListExistImages(image);

                            var count = VisualTreeHelper.GetChildrenCount(groupBox);
                            for (int index = 0; index < count; index++)
                            {
                                var parent = VisualTreeHelper.GetParent(groupBox);


                                var child = VisualTreeHelper.GetChild(groupBox, index);
                                var canvas = child as Canvas;
                                if (canvas != null)
                                {
                                    BitmapImage bitmapImage = new BitmapImage();
                                    var thumbnailImage = image.GetThumbnailImage(20, 20, () => false, IntPtr.Zero);

                                    using (var ms = new MemoryStream())
                                    {
                                        bitmapImage.BeginInit();
                                        thumbnailImage.Save(ms, thumbnailImage.RawFormat);
                                        bitmapImage.StreamSource = ms;
                                        bitmapImage.EndInit();
                                    }

                                    var im = new System.Windows.Controls.Image() { Source = bitmapImage };
                                    canvas.Children.Add(im);
                                }
                            }

                            listView.Items.Add(newFileName);
                        }
                    }
                    catch (Exception)
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
