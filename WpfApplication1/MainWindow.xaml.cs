using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        class rowData
        {
            public string Name { get; private set; }
            public string Type { get; private set; }
            public string Size { get; private set; }
            public string Path { get; private set; }

            public rowData(FileInfo file)
            {
                this.Name = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                this.Path = file.FullName;
                if (file.Attributes != FileAttributes.Directory)
                {
                    this.Type = file.Extension.Substring(1);
                    this.Size = (file.Length / 1024).ToString("#,###,###.#") + " kb";
                }
            }
        }
        public MainWindow() { InitializeComponent(); }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                addFiles(Enumerable.Range(0, a.Length).Select(x => a.GetValue(x).ToString()).ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void addFiles(string[] files)
        {
            if (files == null || files.Length < 1)
                return;

            foreach (var file in files)
            {
                lst_filelist.Items.Add(new rowData(new FileInfo(file)));
            }
        }

        private void btn_work_Click(object sender, RoutedEventArgs evt)
        {
            if (lst_filelist.Items.Count == 0)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Filter = "Archive Files|*.zip;*.rar;*.7z";

                if (dialog.ShowDialog() == true)
                {
                    addFiles(dialog.FileNames);
                }
                return;
             }
            else
            {
                while (lst_filelist.Items.Count > 0)
                {
                    var file = lst_filelist.Items[0] as rowData;

                    lst_filelist.Items.RemoveAt(0);
                    txt_content.Text = file.Name;

                    var batch = new BatchCG();
                    var worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += (s, e) =>
                    {
                        while (!batch.IsEnd)
                        {
                            Thread.Sleep(100);
                            if (batch.FileItems != null) { 
                                (s as BackgroundWorker).ReportProgress((int)batch.Progress);
                                Console.WriteLine("cursor : {0}, / fileitems : {1} ... progress : {2}", batch.Cursor, batch.FileItems.Count(), batch.Progress);
                            }
                        }
                    };

                    worker.ProgressChanged += (s, e) =>
                    {
                        progressBar.Value = (e as ProgressChangedEventArgs).ProgressPercentage;
                    };

                    worker.RunWorkerCompleted+= (s, e) =>
                    {
                        switch (cmb_batch.SelectedIndex)
                        {
                            case 0:
                                Batch_old.ReSize(file.Path, lbl_destination.Content.ToString());
                                break;
                            case 1:
                                Batch_old.Thumbnail(file.Path, lbl_destination.Content.ToString());
                                break;
                            case 2:
                                Batch_old.HCG2(file.Path, lbl_destination.Content.ToString());
                                break;
                            case 3:
                                Batch_old.OnlyImage(file.Path);
                                break;
                            default:
                                break;
                        }
                        txt_content.Text = "";
                    };

                    worker.RunWorkerAsync();

                    batch.Run(file.Path);
                }
            }
        }

        private void lbl_destination_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lbl_destination.Content = dialog.SelectedPath.ToString();
            }
        }

        private void cmb_batch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_batch.SelectedIndex == 0)
            {
                lbl_destination.Content = @"F:\CONTENTS [MAGAZINE]";
            }
            else if (cmb_batch.SelectedIndex == 1)
            {
                lbl_destination.Content = @"H:\CONTENTS [HCG]";
            }
            else if (cmb_batch.SelectedIndex == 2)
            {
                lbl_destination.Content = @"H:\CONTENTS [HCG]";
            }
        }
    }
}