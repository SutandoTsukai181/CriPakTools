using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using Ookii.Dialogs.Wpf;
using System.Threading;
using System.Windows.Threading;
using LibCPK;

namespace CriPakGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetBasicPrefs();
        }

        private void SetBasicPrefs()
        {
            menu_savefiles.IsEnabled = false;
            menu_importAssets.IsEnabled = false;
            progressbar0.Maximum = 100;
            MainApp.Instance.currentPackage.BasePath = @"C:/";
            this.Title = string.Format( $"CRIWARE CPK GUI (JoJo ASBR Ver.)");
        }
        private void menu_openfile_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Loading cpk");
            string fName;
            string baseName;
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.InitialDirectory = "";
            openFileDialog.Filter = "Criware CPK|*.cpk";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog().Value)
            {
                fName = openFileDialog.FileName;
                baseName = System.IO.Path.GetFileName(fName);
                status_cpkname.Content = baseName;
                button_extract.IsEnabled = true;
                button_importassets.IsEnabled = true;
                BeginLoadCPKAsync(fName, () =>
                {

                    CpkWrapper cpk = new CpkWrapper(fName);
                    if (cpk.IsVaidCPK == true)
                    {
                        status_cpkmsg.Content = string.Format("{0} file(s) registered.", cpk.nums);
                        datagrid_cpk.ItemsSource = cpk.table;
                        menu_importAssets.IsEnabled = true;
                        menu_savefiles.IsEnabled = true;
                    }
                    else
                    {
                        status_cpkmsg.Content = string.Format("Not a valid cpk.");
                    }
                });
            }
        }

        private void BeginLoadCPKAsync(string fName, Action onComplete)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.SystemIdle,
                    new Action(() =>
                    {
                        MainApp.Instance.currentPackage.BasePath = System.IO.Path.GetDirectoryName(fName);
                        MainApp.Instance.currentPackage.BaseName = System.IO.Path.GetFileName(fName);
                        MainApp.Instance.currentPackage.FileName = fName;
                        onComplete?.Invoke();
                    })
               );
            });
        }


        private void menu_importAssets_Click(object sender, RoutedEventArgs e)
        {
            CpkPatcher patcherWindow = new CpkPatcher(this.Top, this.Left);
            patcherWindow.ShowDialog();
        }


        private delegate void progressbarDelegate(float no);

        private delegate void datagridDelegate(bool value);

        private void updateDatagrid(bool value)
        {
            datagrid_cpk.IsEnabled = value;
            button_extract.IsEnabled = value;
            button_importassets.IsEnabled = value;
        }

        private void updateprogressbar(float no)
        {
            progressbar0.Value = no;
        }

        private void menu_savefiles_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog saveFilesDialog = new VistaFolderBrowserDialog();
            saveFilesDialog.SelectedPath = MainApp.Instance.currentPackage.BasePath;
            if (saveFilesDialog.ShowDialog().Value)
            {
                Debug.Print(saveFilesDialog.SelectedPath + "/" + MainApp.Instance.currentPackage.BaseName + "_unpacked");
                ThreadPool.QueueUserWorkItem(new WaitCallback(beginExtractCPK), saveFilesDialog.SelectedPath);

            }

        }

        private void beginExtractCPK(object fOutDir)
        {
            try
            {
                beginExtractCPK_internal(fOutDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format($"An Error occured while extract cpk.\n {ex.Message}"));
            }
        }
        private void beginExtractCPK_internal(object fOutDir)
        {
            string outDir;
            outDir = (string)(fOutDir + "/" + MainApp.Instance.currentPackage.BaseName + "_unpacked");
            if (MainApp.Instance.currentPackage.CpkContent != null)
            {
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }
                BinaryReader oldFile = new BinaryReader(File.OpenRead(MainApp.Instance.currentPackage.CpkContentName));
                List<FileEntry> entries = null;

                entries = MainApp.Instance.currentPackage.CpkContent.fileTable.Where(x => x.FileType == "FILE").ToList();

                if (entries.Count == 0)
                {
                    Debug.Print("err while extracting.");
                    oldFile.Close();
                    return;
                }

                int i = 0;
                int id;
                string currentName;
                bool bFileRepeated = Tools.CheckListRedundant(entries);
                this.Dispatcher.Invoke(new datagridDelegate(updateDatagrid), new object[] { (bool)false });

                while (i < entries.Count)
                {
                    this.Dispatcher.Invoke(new progressbarDelegate(updateprogressbar), new object[] { (float)i / (float)entries.Count * 100f });

                    if (!String.IsNullOrEmpty((string)entries[i].DirName))
                    {
                        Directory.CreateDirectory(outDir + "/" + entries[i].DirName.ToString());
                    }

                    id = Convert.ToInt32(entries[i].ID);
                    if (id > 0 && bFileRepeated)
                    {
                        currentName = (((entries[i].DirName != null) ?
                                        entries[i].DirName + "/" : "") + string.Format("[{0}]", id.ToString()) + entries[i].FileName);
                        currentName = currentName.TrimStart('/');
                    }
                    else
                    {
                        currentName = ((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName;
                        currentName = currentName.TrimStart('/');
                    }

                    int file_size = Int32.Parse(entries[i].FileSize.ToString());
                    oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                    byte[] chunk = Tools.CryptJoJoASBR(oldFile.ReadBytes(file_size), (uint)file_size);
                    
                    if (Encoding.ASCII.GetString(chunk, 0, 8) == "CRILAYLA")
                    {
                        int size = Int32.Parse((entries[i].ExtractSize ?? entries[i].FileSize).ToString());

                        if (size != 0)
                        {
                            chunk = MainApp.Instance.currentPackage.CpkContent.DecompressLegacyCRI(chunk, size);
                        }
                    }

                    Debug.WriteLine(" FileName :{0}\n    FileOffset:{1:x8}    ExtractSize:{2:x8}   ChunkSize:{3:x8} {4}",
                                                                entries[i].FileName.ToString(),
                                                                (long)entries[i].FileOffset,
                                                                entries[i].ExtractSize,
                                                                entries[i].FileSize,
                                                                ((float)i / (float)entries.Count) * 100f);
                    string dstpath = outDir + "/" + currentName;
                    dstpath = Tools.GetSafePath(dstpath);
                    string dstdir = System.IO.Path.GetDirectoryName(dstpath);
                    if (!Directory.Exists(dstdir))
                    {
                        Directory.CreateDirectory(dstdir);
                    }
                    File.WriteAllBytes(dstpath, chunk);
                    i += 1;
                }
                oldFile.Close();
                this.Dispatcher.Invoke(new progressbarDelegate(updateprogressbar), new object[] { 100f });
                this.Dispatcher.Invoke(new datagridDelegate(updateDatagrid), new object[] { (bool)true });
                GC.Collect();
                MessageBox.Show("Extraction Complete.");
            }

        }

        private void button_extract_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog saveFilesDialog = new VistaFolderBrowserDialog();
            saveFilesDialog.SelectedPath = MainApp.Instance.currentPackage.BasePath + "/";
            if (saveFilesDialog.ShowDialog().Value)
            {
                Debug.Print(saveFilesDialog.SelectedPath + "/" + MainApp.Instance.currentPackage.BaseName + "_unpacked");
                ThreadPool.QueueUserWorkItem(new WaitCallback(beginExtractCPK), saveFilesDialog.SelectedPath);

            }
        }

        private void button_importassets_Click(object sender, RoutedEventArgs e)
        {
            CpkPatcher patcherWindow = new CpkPatcher(this.Top, this.Left);
            patcherWindow.ShowDialog();


        }

        private void menu_aboutgui_Click(object sender, RoutedEventArgs e)
        {
            WindowAboutGUI aboutwindow = new WindowAboutGUI(this.Top, this.Left);
            aboutwindow.ShowDialog();
        }

        private void dgmenu1_Cilck(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this.datagrid_cpk);
            HitTestResult htr = VisualTreeHelper.HitTest(this.datagrid_cpk, p);
            TextBlock o = htr.VisualHit as TextBlock;
            if (o != null)
            {
                DataGridRow dgr = VisualTreeHelper.GetParent(o) as DataGridRow;

                dgr.Focus();
                dgr.IsSelected = true;
            }
        }
        private void dgitem1_Click(object sender, RoutedEventArgs e)
        {

            CPKTable t = this.datagrid_cpk.SelectedItem as CPKTable;
            if (t != null)
            {
                if (t.FileSize > 0 && t.FileType == "FILE")
                {
                    VistaSaveFileDialog saveFilesDialog = new VistaSaveFileDialog();
                    saveFilesDialog.InitialDirectory = MainApp.Instance.currentPackage.BasePath;
                    saveFilesDialog.FileName = MainApp.Instance.currentPackage.BasePath + "/" + t.LocalName;
                    if (saveFilesDialog.ShowDialog().Value)
                    {
                        byte[] chunk = ExtractItem(t);

                        File.WriteAllBytes(saveFilesDialog.FileName, chunk);
                        MessageBox.Show(String.Format("Decompress to :{0}", saveFilesDialog.FileName));
                    }

                }
            }

        }

        private void dgitem2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Currently not supported");
        }

        private byte[] ExtractItem(CPKTable t)
        {
            CPKTable entries = t as CPKTable;
            BinaryReader oldFile = new BinaryReader(File.OpenRead(MainApp.Instance.currentPackage.CpkContentName));

            int file_size = Int32.Parse(entries.FileSize.ToString());
            oldFile.BaseStream.Seek((long)entries.FileOffset, SeekOrigin.Begin);
            byte[] chunk = Tools.CryptJoJoASBR(oldFile.ReadBytes(file_size), (uint)file_size);

            if (Encoding.ASCII.GetString(chunk, 0, 8) == "CRILAYLA")
            {
                int size;
                if (entries.ExtractSize == 0)
                {
                    size = entries.FileSize;
                }
                else
                {
                    size = entries.ExtractSize;
                }

                if (size != 0)
                {
                    chunk = MainApp.Instance.currentPackage.CpkContent.DecompressLegacyCRI(chunk, size);
                }
            }
            oldFile.Close();
            return chunk;




        }

        private void menu_makeCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("currently not supported");
        }

        private void comboBox_encodings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int cur = comboBox_encodings.SelectedIndex;
            Encoding current_codepage;
            switch (cur)
            {
                case 0:
                    current_codepage = Encoding.GetEncoding((int)PackageEncodings.UTF_8);
                    break;
                case 1:
                    current_codepage = Encoding.GetEncoding((int)PackageEncodings.SHIFT_JIS);
                    break;
                default:
                    current_codepage = Encoding.GetEncoding((int)PackageEncodings.UTF_8);
                    break;

            }
            if (current_codepage != MainApp.Instance.currentPackage.EncodingPage)
            {
                MainApp.Instance.currentPackage.EncodingPage = current_codepage;
                if (MainApp.Instance.currentPackage.FileName != null)
                {
                    var fName = MainApp.Instance.currentPackage.FileName;
                    BeginLoadCPKAsync(fName, () =>
                    {
                        CpkWrapper cpk = new CpkWrapper(fName);
                        status_cpkmsg.Content = string.Format("{0} file(s) registered.", cpk.nums);
                        datagrid_cpk.ItemsSource = cpk.table;
                        menu_importAssets.IsEnabled = true;
                        menu_savefiles.IsEnabled = true;
                    });
                }

            }
        }
    }
}
