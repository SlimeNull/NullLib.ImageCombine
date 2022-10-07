using Microsoft.Win32;
using NullLib;
using NullLib.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageCombineGui
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = (string[])(System.Array)e.Data.GetData(DataFormats.FileDrop);
                foreach(string i in filenames)
                {
                    FilesBox.Items.Add(i);
                }
            }
        }

        OpenFileDialog ofd;
        private void Append_Click(object sender, RoutedEventArgs e)
        {
            if (ofd == null)
            {
                ofd = new OpenFileDialog();
                ofd.Title = "打开一个有序的图像文件";
                ofd.Filter = "常用图像格式|*.jpg;*.jpeg;*.png;*.gif;*.bmp|JPEG|*.jpg;*.jpeg|PNG|*.png|GIF|*.gif|位图|*.bmp|其他|*.*";
                ofd.CheckFileExists = true;
                ofd.Multiselect = true;
            }
            if (ofd.ShowDialog().GetValueOrDefault(false))
            {
                foreach (string i in ofd.FileNames)
                {
                    FilesBox.Items.Add(i);
                }
            }

            GlobalTip.Content = "打开了图像";
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (FilesBox.SelectedIndex >= 0)
            {
                FilesBox.Items.RemoveAt(FilesBox.SelectedIndex);
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (FilesBox.SelectedIndex > 0)
            {
                int index = FilesBox.SelectedIndex;
                object temp = FilesBox.Items[index];
                FilesBox.Items[index] = FilesBox.Items[index - 1];
                FilesBox.Items[index - 1] = temp;
                FilesBox.SelectedIndex = index - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (FilesBox.SelectedIndex < FilesBox.Items.Count - 1)
            {
                int index = FilesBox.SelectedIndex;
                object temp = FilesBox.Items[index];
                FilesBox.Items[index] = FilesBox.Items[index + 1];
                FilesBox.Items[index + 1] = temp;
                FilesBox.SelectedIndex = index + 1;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FilesBox.Items.Clear();
        }

        void UpdateGlobalTip(DealState dealState, int progress, int progressEnd)
        {
            Dispatcher.Invoke(() =>
            {
                switch (dealState)
                {
                    case DealState.ProcessParameters:
                        GlobalTip.Content = $"图像拼合: 正在处理参数";
                        break;
                    case DealState.ProcessLoadImages:
                        GlobalTip.Content = $"图像拼合: 正在载入图像, 进度:{progress}/{progressEnd}";
                        break;
                    case DealState.ProcessImageSizes:
                        GlobalTip.Content = $"图像拼合: 正在处理图像尺寸信息, 进度:{progress}/{progressEnd}";
                        break;
                    case DealState.ProcessDrawing:
                        GlobalTip.Content = $"图像拼合: 正在绘图中, 进入:{progress}/{progressEnd}";
                        break;
                    case DealState.ProcessReturn:
                        GlobalTip.Content = $"图像拼合: 正在返回结果";
                        break;
                    case DealState.ProcessSave:
                        GlobalTip.Content = $"图像拼合: 正在保存图像至存储设备";
                        break;
                }
            });
        }
        bool dealing = false;
        bool Dealing
        {
            get
            {
                return dealing;
            }
            set
            {
                if (value)
                {
                    DealButton.Content = "强制停止";
                }
                else
                {
                    DealButton.Content = "处理图像";
                }
            }
        }
        Thread CombineActionThread;
        BitmapImage previewImageSource;
        Bitmap outputBmp;
        void CombineAction()
        {
            string[] files = null;
            bool hfirst = true, mainR = false, crossR = false;
            Dispatcher.Invoke(()=>
            {
                files = new string[FilesBox.Items.Count];
                FilesBox.Items.CopyTo(files, 0);

                hfirst = HDirection.IsChecked.GetValueOrDefault(false);
                mainR = MainReverse.IsChecked.GetValueOrDefault(false);
                crossR = CrossReverse.IsChecked.GetValueOrDefault(false);
            });
            ImageCombineLib.Combine(files, UpdateGlobalTip, out outputBmp, horizontalFirst: hfirst, mainReverse: mainR, crossReverse: crossR, format: System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Dispatcher.Invoke(() =>
            {
                if (outputBmp is null)
                {
                    GlobalTip.Content = "图像拼合: 处理错误, 结果为空";
                    return;
                }

                previewImageSource = new BitmapImage();
                previewImageSource.BeginInit();
                previewImageSource.StreamSource = new MemoryStream();
                outputBmp.Save(previewImageSource.StreamSource, ImageFormat.Bmp);
                previewImageSource.EndInit();

                Dealing = false;
                PreviewImage.Source = previewImageSource;
                GlobalTip.Content = $"图像拼合: 任务完成";
            });
        }
        private void Deal_Click(object sender, RoutedEventArgs e)
        {
            if (dealing)
            {
                CombineActionThread.Abort();
                Dealing = false;
            }
            else
            {
                if (previewImageSource != null)
                {
                    PreviewImage.Source = null;
                    previewImageSource.StreamSource.Dispose();
                    previewImageSource = null;
                    GC.Collect();
                }

                Dealing = true;
                CombineActionThread = new Thread(CombineAction);
                CombineActionThread.Start();
            }
        }

        SaveFileDialog sfd;
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (outputBmp is null)
            {
                MessageBox.Show("请在保存前处理图像!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (sfd == null)
            {
                sfd = new SaveFileDialog();
                sfd.FileName = "output.png";
                sfd.Title = "保存到文件";
                sfd.Filter = "常用图像格式|*.jpg;*.jpeg;*.png;*.gif;*.bmp|JPEG|*.jpg;*.jpeg|PNG|*.png|GIF|*.gif|位图|*.bmp|其他|*.*";
            }
            if (sfd.ShowDialog().GetValueOrDefault(false))
            {
                outputBmp.Save(sfd.FileName);
            }

            GlobalTip.Content = "保存了图像";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (previewImageSource != null)
            {
                PreviewImage.Source = null;
                previewImageSource.StreamSource.Dispose();
                previewImageSource = null;
                GC.Collect();
            }
        }
    }
}
