using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Image = System.Drawing.Image;
using Icon = System.Drawing.Icon;
using Timer = System.Timers.Timer;
using System.Drawing.Imaging;
using System.Drawing;

namespace WallpapersMoveToDesktop
{
    public partial class MainWindow : Window
    {
        #region Fields and Properties

        private NotifyIcon _notifyIcon;
        private List<string> _existsImages = new List<string>();
        private Timer _timer = new Timer(30 * 60 * 1000); // 30 minutes
        int _lastHour = DateTime.Now.Hour;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            SetIconToMainApplication();
            LoadExistsImages();

            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Start();            
        }

        private void SetIconToMainApplication()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = Properties.Resources.ResourceManager.GetObject("ApplicationMainIcon") as Icon;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += delegate(object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
        }

        private void ExecutingLoadingImagesEveryHour()
        {
            button.IsEnabled = false;
            listView.Items.Clear();

            if (!string.IsNullOrEmpty(EnvVariables.SrcPath))
            {
                // Chech destanation directory (create if not exists)
                this.ChechDestDirectory();
                // Get source directory information
                DirectoryInfo srcDirInfo = new DirectoryInfo(EnvVariables.SrcPath);
                // get all files from source directory
                FileInfo[] fileInfos = srcDirInfo.GetFiles();
                // Index of image on UI
                int index = 0;
                
                foreach (var fileInfo in fileInfos)
                {
                    try
                    {
                        var oldFileName = Path.Combine(EnvVariables.SrcPath, fileInfo.Name);
                        var image = Image.FromFile(oldFileName);

                        if (image.Height >= Constants.FullHdHeight && image.Width >= Constants.FullHdWidth)
                        {
                            var newFileName = Path.Combine(EnvVariables.DestPath, string.Format(fileInfo.Name + ".jpeg"));
                            if (File.Exists(newFileName) || ContainsImage(image))
                            {
                                continue;
                            }

                            File.Copy(oldFileName, newFileName);
                            AddToListExistImages(image);

                            var canvas = FindVisualChildren<Canvas>(groupBox).FirstOrDefault();

                            if (canvas != null)
                            {
                                BitmapImage bitmapImage = GetThumbnailBitmapImage(newFileName);
                                var uiElement = new System.Windows.Controls.Image() { Source = bitmapImage };

                                Canvas.SetLeft(uiElement, GetIndentLeft(index));
                                Canvas.SetTop(uiElement, GetIndentTop(index));

                                canvas.Children.Add(uiElement);
                                index++;
                            }

                            listView.Items.Add(newFileName);
                        }
                    }
                    catch (Exception)
                    {
                        // MessageBox.Show(exception.Message);
                    }
                }
            }

            button.IsEnabled = true;
        }

        private static int GetIndentTop(int index)
        {
            return (index / Constants.ImagesRowCount) * (Constants.ThumbnailHeight + Constants.EmptySpaceLength) + Constants.EmptySpaceLength;
        }

        private static int GetIndentLeft(int index)
        {
            var rowIndentCoeff = (index + 1) % 10 == 0 ? (index + 1) / 10 - 1 : (index + 1) / 10;
            return index * (Constants.ThumbnailWidth + Constants.EmptySpaceLength) + Constants.EmptySpaceLength
                - (Constants.ThumbnailWidth + Constants.EmptySpaceLength) * Constants.ImagesRowCount * rowIndentCoeff;
        }

        private BitmapImage GetThumbnailBitmapImage(string fileName)
        {
            BitmapImage thumbnailBitmapImage = new BitmapImage();

            using (var memoryStream = new MemoryStream())
            {
                Image thumbnailSource = new Bitmap(fileName);
                Image thumbnail = thumbnailSource.GetThumbnailImage(
                    Constants.ThumbnailWidth,
                    Constants.ThumbnailHeight,
                    () => false,
                    IntPtr.Zero);
                thumbnail.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Seek(0, SeekOrigin.Begin);

                thumbnailBitmapImage.BeginInit();
                thumbnailBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                thumbnailBitmapImage.StreamSource = memoryStream;
                thumbnailBitmapImage.EndInit();
            }

            return thumbnailBitmapImage;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (_lastHour < DateTime.Now.Hour || (_lastHour == 23 && DateTime.Now.Hour == 0))
            {
                _lastHour = DateTime.Now.Hour;
                ExecutingLoadingImagesEveryHour();
            }
        }

        private void LoadExistsImages()
        {
            _existsImages.Clear();

            DirectoryInfo destDirInfo = this.GetDestDirectory();
            FileInfo[] fileInfos = destDirInfo.GetFiles();

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    var fileName = Path.Combine(EnvVariables.DestPath, fileInfo.Name);
                    var image = Image.FromFile(fileName);

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

        private DirectoryInfo GetDestDirectory()
        {
            this.ChechDestDirectory();
            return new DirectoryInfo(EnvVariables.DestPath);
        }

        private void ChechDestDirectory()
        {
            if (!Directory.Exists(EnvVariables.DestPath))
            {
                Directory.CreateDirectory(EnvVariables.DestPath);
            }
        }

        private void AddToListExistImages(Image image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, image.RawFormat);
                _existsImages.Add(Convert.ToBase64String(memoryStream.ToArray()));
            }
        }

        private bool ContainsImage(Image originalImage)
        {
            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    originalImage.Save(memoryStream, originalImage.RawFormat);
                    var stringImage = Convert.ToBase64String(memoryStream.ToArray());
                    if (!_existsImages.Contains(stringImage))
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return true;
        }

        public static List<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            List<T> foundChilds = new List<T>();
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                T childType = child as T;
                if (childType == null)
                {
                    var result = FindVisualChildren<T>(child);
                    if (result.Any())
                    {
                        foundChilds.AddRange(result);
                        break;
                    }
                }
                else
                {
                    foundChilds.Add((T)child);
                }
            }

            return foundChilds;
        }
        
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ExecutingLoadingImagesEveryHour();
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
