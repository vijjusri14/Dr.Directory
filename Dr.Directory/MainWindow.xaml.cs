#region NameSpaces

using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO;
using System.Security.AccessControl;
using System.ComponentModel;
using System.Threading.Tasks;

#endregion
namespace Dr.Directory
{
    public partial class MainWindow : Window
    {
        #region Variables

        BackgroundWorker bw = new BackgroundWorker();
        StringBuilder csv = new StringBuilder(1024, 2147483647);
        string parent = "";
        string filter = "";
        string filterUnit = "";
        int total = 0, part = 0;
        bool filterenabled = false;
        bool Canceled = false;

        #endregion

        #region Main

        public MainWindow()
        {
            InitializeComponent();

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork +=new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged +=new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted +=new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
        }

        #endregion

        #region BrowseButtonClick

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FileSave.Visibility = Visibility.Collapsed;
            CancelExport.Visibility = Visibility.Collapsed;
            Progress.Text = "";
            FolderBrowserDialog folderdialog = new FolderBrowserDialog();
            folderdialog.SelectedPath = "C:\\";
            DialogResult result = folderdialog.ShowDialog();
            if (result.ToString()=="OK")
            {
                SolidColorBrush FolderColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFC107"));
                label2.Foreground = FolderColor;
                label2.Content = folderdialog.SelectedPath;
                parent = label2.Content.ToString();
                ExportButton.IsEnabled = true;
            }
        }

        #endregion

        #region ExportButtonClick

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Canceled = false;
            BrowseButton.IsEnabled = false;
            ExportButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;
            CancelButton.IsEnabled = true;
            FileSave.Visibility = Visibility.Collapsed;
            CancelExport.Visibility = Visibility.Collapsed;
            filter = FilterSize.Text.ToString();
            filterUnit = ((ComboBoxItem)SelectUnit.SelectedItem).Tag.ToString();
            filterenabled = (bool)FilterStatus.IsChecked;
            total = System.IO.Directory.GetDirectories(parent, "*", SearchOption.AllDirectories).Count();
            if (!bw.IsBusy)
            {
                part = 0;
                bw.RunWorkerAsync();
            }
        }

        #endregion

        #region CancelButtonClick

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (bw.IsBusy)
            {
                bw.CancelAsync();
            }
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Collapsed;
            ExportButton.Visibility = Visibility.Visible;
            ExportButton.IsEnabled = true;
        }

        #endregion

        #region CloseButtonClick

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        #endregion

        #region BackgroundWorkerDoWork

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            DriveInfo Drive = new DriveInfo(parent);
            DirectoryInfo di = new DirectoryInfo(parent);
            string filePath = @"C:\Users\" + Environment.UserName + @"\desktop\"+di.Name+"_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
            csv.AppendLine(@"Host_Name,Directory_Path,Parent_Directory,Current_Directory,Directory_Size,Directory_Size_in_bytes,Directory_Size_in_GB,Created_On,Owner,User_Access");
            string ug = "";
            //long dirlen = FileSizeRecursive(parent, 0L);
            long dirlen = DirSize.SizeDir(parent);
            float size = dirlen;
            string unit = "";
            var Owner = di.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount));
            AuthorizationRuleCollection ac1 = di.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            Parallel.ForEach(ac1.OfType<AuthorizationRule>(), ace =>
            {
                //if (ace.IdentityReference.ToString() == @"BUILTIN\Administrators" || ace.IdentityReference.ToString() == @"BUILTIN\Users" || ace.IdentityReference.ToString() == @"NT AUTHORITY\SYSTEM" || ace.IdentityReference.ToString() == @"NT AUTHORITY\Authenticated Users" || ace.IdentityReference.ToString() == "CREATOR OWNER")
                //{
                //}
                //else
                //{
                    if (ug != "")
                        ug += "\r\n" + ace.IdentityReference.ToString();
                    else
                        ug += ace.IdentityReference.ToString();
                //}
            });
            if (dirlen < 1024)
            {
                size = (float)dirlen;
                unit = "bytes";
            }
            if (dirlen >= 1024 && dirlen < 1048576)
            {
                size = (float)dirlen / 1024;
                unit = "KB";
            }
            if (dirlen > 1048576 && dirlen < 1073741824)
            {
                size = (float)dirlen / 1048576;
                unit = "MB";
            }
            if (dirlen > 1073741824 && dirlen < 1099511627776)
            {
                size = (float)dirlen / 1073741824;
                unit = "GB";
            }
            var strfmt = string.Format(@"{0}," + "\"{9}\",\"{1}\",\"{2}\",{4} {5},{3},{6} GB,{7},{10},\"{8}\"", Environment.MachineName, di.Parent.ToString() == "" ? Drive.Name.ToString() : di.Parent.ToString(), di.Name, dirlen, size.ToString("#0.00"), unit, ((float)dirlen / 1073741824).ToString("#0.00"), di.CreationTime, ug == "" ? "Everyone" : ug, parent, Owner);
            int FS = filter != "" ? int.Parse(filter) : 0;
            long SU = long.Parse(filterUnit);
            var dirdetails = filterenabled == true ? ((FS * SU >= dirlen) ? null : strfmt) : strfmt;
            if (dirdetails != null)
                csv.AppendLine(dirdetails);
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            else
            {
                WriteCSV(parent);
            }
            if (!Canceled)
            {
                File.AppendAllText(filePath, csv.ToString());                
            }
            csv.Clear();
        }

        #endregion

        #region BackgroundWorkerRunWorkCompleated

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Canceled)
            {
                FileSave.Visibility = Visibility.Collapsed;
                CancelExport.Visibility = Visibility.Visible;
            }
            else if (e.Error != null)
            {

            }
            else
            {
                CancelExport.Visibility = Visibility.Collapsed;
                FileSave.Visibility = Visibility.Visible;
            }
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Collapsed;
            BrowseButton.IsEnabled = true;
            ExportButton.Visibility = Visibility.Visible;
            ExportButton.IsEnabled = true;
        }

        #endregion

        #region BackgroundWorkerProgressChanged

        public void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress.Text = e.ProgressPercentage.ToString() + "%";
        }

        #endregion

        #region WriteCSV

        private void WriteCSV(string path)
        {
            if (bw.CancellationPending)
            {
                Canceled = true;
                return;
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] dirDirs = di.GetDirectories();
                part++;
                bw.ReportProgress((part - 1) * 100 / total);
                string ug = "";
                //long dirlen = FileSizeRecursive(parent, 0L);
                long dirlen = DirSize.SizeDir(parent);
                var Owner = di.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount));
                AuthorizationRuleCollection ac1 = di.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                Parallel.ForEach(ac1.OfType<AuthorizationRule>(), ace =>
                {
                    //if (ace.IdentityReference.ToString() == @"BUILTIN\Administrators" || ace.IdentityReference.ToString() == @"BUILTIN\Users" || ace.IdentityReference.ToString() == @"NT AUTHORITY\SYSTEM" || ace.IdentityReference.ToString() == @"NT AUTHORITY\Authenticated Users" || ace.IdentityReference.ToString() == "CREATOR OWNER")
                    //{
                    //}
                    //else
                    //{
                        if (ug != "")
                            ug += "\r\n" + ace.IdentityReference.ToString();
                        else
                            ug += ace.IdentityReference.ToString();
                    //}
                });
                if (dirDirs.Length == 0)
                {
                }
                else
                {
                    Parallel.ForEach(dirDirs, dir =>
                    {
                        //long sdirlen = FileSizeRecursive(dir.FullName.ToString(), 0L);
                        long sdirlen = DirSize.SizeDir(dir.FullName);
                        float size = sdirlen;
                        string unit = "";
                        if (sdirlen < 1024)
                        {
                            size = (float)sdirlen;
                            unit = "bytes";
                        }
                        if (sdirlen >= 1024 && sdirlen < 1048576)
                        {
                            size = (float)sdirlen / 1024;
                            unit = "KB";
                        }
                        if (sdirlen > 1048576 && sdirlen < 1073741824)
                        {
                            size = (float)sdirlen / 1048576;
                            unit = "MB";
                        }
                        if (sdirlen > 1073741824 && sdirlen < 1099511627776)
                        {
                            size = (float)sdirlen / 1073741824;
                            unit = "GB";
                        }
                        var strfmt = string.Format(@"{0},"+"\"{9}\",\"{1}\",\"{2}\",{4} {5},{3},{6} GB,{7},{10},\"{8}\"", Environment.MachineName, dir.Parent, dir.Name, sdirlen, size.ToString("#0.00"), unit, ((float)sdirlen / 1073741824).ToString("#0.00"), dir.CreationTime, ug == "" ? "Everyone" : ug, dir.FullName.ToString(), Owner);
                        int FS = filter != "" ? int.Parse(filter) : 0;
                        long SU = long.Parse(filterUnit);
                        var sdirdetails = filterenabled == true ? ((FS * SU >= sdirlen) ? null : strfmt) : strfmt;
                        if (sdirdetails != null)
                            csv.AppendLine(sdirdetails);
                        WriteCSV(dir.FullName.ToString());
                    });
                }
            }
        }

        #endregion

        #region GetFileSizeDirectory

        //private long GetFileSizeDirectory(string directory)
        //{
        //    long dirLength = 0;
        //    DirectoryInfo di = new DirectoryInfo(directory);
        //    FileInfo[] dirFiles = di.GetFiles();
        //    Parallel.ForEach(dirFiles, fi =>
        //    {
        //        dirLength += fi.Length;
        //    });
        //    return dirLength;
        //}

        #endregion

        #region FileSizeRecursive

        //private long FileSizeRecursive(string subdir, long p_dirln)
        //{
        //    DirectoryInfo di = new DirectoryInfo(subdir);
        //    DirectoryInfo[] dirDirs = di.GetDirectories();
        //    long dirln = p_dirln;
        //    dirln += GetFileSizeDirectory(subdir);
        //    if (dirDirs.Length == 0)
        //    {
        //        return dirln;
        //    }
        //    else
        //    {
        //        Parallel.ForEach(dirDirs, dir =>
        //        {
        //            dirln = FileSizeRecursive(dir.FullName, dirln);
        //        });
        //        return dirln;
        //    }
        //}

        #endregion

        #region FilterSize

        private void FilterSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox textbox = sender as System.Windows.Controls.TextBox;
            int selectionstart = textbox.SelectionStart;
            int selectionlength = textbox.SelectionLength;
            string newText = string.Empty;
            foreach (char c in textbox.Text.ToCharArray())
            {
                if (char.IsDigit(c) || char.IsControl(c))
                    newText += c;
            }
            textbox.Text = newText;
            textbox.SelectionStart = selectionstart <= textbox.Text.Length ? selectionstart : textbox.Text.Length;
        }

        #endregion
    }
}