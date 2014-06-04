// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineCounterMain.xaml.cs" company="FreeToDev"> (c) Mike Fourie. All other rights reserved.</copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LineCounter.UserControls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for LineCounter
    /// </summary>
    public partial class LineCounterMain
    {
        private MainWindow parentWindow;
        private string path;
        private List<CsvFile> csvFiles = new List<CsvFile>(); 
        private ObservableCollection<FileCategory> cats = new ObservableCollection<FileCategory>();
        private ObservableCollection<FileReport> reportedFiles = new ObservableCollection<FileReport>();
        private ObservableCollection<IgnoredFile> ignoredFiles = new ObservableCollection<IgnoredFile>();
        private ObservableCollection<IgnoredExtension> ignoredExtensions = new ObservableCollection<IgnoredExtension>();
        private SearchOption recursiveSearch = SearchOption.TopDirectoryOnly;
        private double smallerThan;
        private double largerThan;
        private IEnumerable<FileInfo> foundFiles;
        private int tfiles;
        private int tlines;
        private int tincluded;
        private int texcluded;
        private double tcomments;
        private double tcode;
        private double tempty;

        public LineCounterMain()
        {
            InitializeComponent();
            this.dgFileCategories.AutoGeneratingColumn += this.dataGrid_AutoGeneratingColumn;
            this.dataGridIgnoredExtensions.AutoGeneratingColumn += this.dataGrid3_AutoGeneratingColumn;
            this.dataGridIgnoredFiles.AutoGeneratingColumn += this.dataGrid2_AutoGeneratingColumn;
            this.dataGridSummary.AutoGeneratingColumn += this.dataGrid4_AutoGeneratingColumn;
        }

        public ObservableCollection<IgnoredExtension> IgnoredExtensions
        {
            get { return this.ignoredExtensions; }
            set { this.ignoredExtensions = value; }
        }

        public ObservableCollection<IgnoredFile> IgnoredFiles
        {
            get { return this.ignoredFiles; }
            set { this.ignoredFiles = value; }
        }

        public ObservableCollection<FileReport> ReportedFiles
        {
            get { return this.reportedFiles; }
            set { this.reportedFiles = value; }
        }

        public ObservableCollection<FileCategory> Categories
        {
            get { return this.cats; }
        }

        public void OpenJson(string filePath)
        {
            FileInfo f = new FileInfo(filePath);
            if (File.Exists(f.FullName))
            {
                this.cats = JsonConvert.DeserializeObject<ObservableCollection<FileCategory>>(File.ReadAllText(f.FullName));
                this.dgFileCategories.ItemsSource = this.cats;
            }
        }

        internal void LoadCategories(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.cats = JsonConvert.DeserializeObject<ObservableCollection<FileCategory>>(File.ReadAllText(fileName));
                this.dgFileCategories.ItemsSource = this.cats;
            }
        }

        internal void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FileCategory cat in this.cats)
            {
                var isChecked = this.chkCategories.IsChecked;
                if (isChecked != null)
                {
                    cat.Include = isChecked.Value;
                }
            }

            this.dgFileCategories.Items.Refresh();
        }

        internal void CopyResults()
        {
            string details = "Path: " + this.parentWindow.txtPath.Text + "\r\n\r\n";
            details += "LINES \r\n------------------------------\r\n";
            details += "Empty:\t\t" + this.tempty + "\r\n";
            details += "Comments:\t" + this.tcomments + "\r\n";
            details += "Code:\t\t" + this.tcode + "\r\n";
            details += "Total:\t\t" + this.tlines + "\r\n\r\n";
            details += "FILES \r\n------------------------------\r\n";
            details += "Total:\t\t" + this.tfiles + "\r\n\r\n";
            details += "FILE CATEGORIES \r\n------------------------------------------------------------------------------------------------------------------------------------------- \r\n";
            details += "Name\t\t\t\tTotal Lines\t\tTotal Files\t\tCode\t\t\tComments\t\tEmpty\t\t\tExtensions\r\n";
            details += "------------------------------------------------------------------------------------------------------------------------------------------- \r\n";
            details = this.Categories.Where(fc => fc.Include || fc.Category == "TOTAL").Aggregate(details, (current, fc) => current + (fc.Category + new string('\t', 5 - (fc.Category.Length / 4)) + fc.TotalLines + new string('\t', 4 - (fc.TotalLines.ToString(CultureInfo.CurrentCulture).Length / 4)) + fc.TotalFiles + new string('\t', 4 - (fc.TotalFiles.ToString(CultureInfo.CurrentCulture).Length / 4)) + fc.Code + new string('\t', 4 - (fc.Code.ToString(CultureInfo.CurrentCulture).Length / 4)) + fc.Comments + new string('\t', 4 - (fc.Comments.ToString(CultureInfo.CurrentCulture).Length / 4)) + fc.Empty + new string('\t', 4 - (fc.Empty.ToString(CultureInfo.CurrentCulture).Length / 4)) + fc.FileTypes + "\r\n"));
            details += "\r\n" + this.parentWindow.txtStatus.Text + "\r\n";
            System.Windows.Forms.Clipboard.SetText(details);
        }

        internal void Scan()
        {
            this.dgFileCategories.CommitEdit();
            this.ignoredExtensions.Clear();
            this.ignoredFiles.Clear();

            foreach (FileCategory fc in this.Categories)
            {
                if (fc.Category == "TOTAL")
                {
                    this.Categories.Remove(fc);
                    break;
                }
            }

            var isChecked = this.parentWindow.chkRecursive.IsChecked;
            if (isChecked != null && isChecked.Value)
            {
                this.recursiveSearch = SearchOption.AllDirectories;
            }
            else
            {
                this.recursiveSearch = SearchOption.TopDirectoryOnly;
            }

            this.path = this.parentWindow.txtPath.Text;
            string rootPath = this.path.Replace("*", string.Empty);
            DirectoryInfo dir = new DirectoryInfo(rootPath);
            this.foundFiles = dir.GetFiles("*", this.recursiveSearch).Where(x => (x.Attributes & FileAttributes.Hidden) == 0);

            this.largerThan = !string.IsNullOrWhiteSpace(this.parentWindow.cboLargerThan.Text) ? Convert.ToDouble(this.parentWindow.cboLargerThan.Text, CultureInfo.CurrentCulture) : 0;
            this.smallerThan = !string.IsNullOrWhiteSpace(this.parentWindow.cboSmallerThan.Text) ? Convert.ToDouble(this.parentWindow.cboSmallerThan.Text, CultureInfo.CurrentCulture) : 0;

            System.Threading.Tasks.Task[] tasks = new System.Threading.Tasks.Task[this.Categories.Count];
            ObservableCollection<FileReport> xxx = new ObservableCollection<FileReport>();
            string fileexclusions = this.parentWindow.txtNameExclusions.Text;
            string folderexclusions = this.parentWindow.txtFolderNameExclusions.Text;
            
            for (int i = 0; i < this.Categories.Count(); i++)
            {
                int i1 = i;
                tasks[i] = System.Threading.Tasks.Task.Factory.StartNew(() => this.ProcessPath(this.Categories[i1], this.foundFiles, xxx, fileexclusions, folderexclusions));
            }

            // Block until all tasks complete.
            System.Threading.Tasks.Task.WaitAll(tasks);
            this.ReportedFiles = xxx;
            this.dataGridSummary.ItemsSource = this.reportedFiles;

            this.tfiles = 0;
            this.tlines = 0;
            this.tincluded = 0;
            this.texcluded = 0;
            this.tcomments = 0;
            this.tcode = 0;
            this.tempty = 0;
            foreach (FileCategory f in this.Categories)
            {
                this.tfiles += f.TotalFiles;
                this.tlines += f.TotalLines;
                this.tcomments += f.Comments;
                this.tempty += f.Empty;
                this.tcode += f.Code;
                this.tincluded += f.IncludedFiles;
                this.texcluded += f.ExcludedFiles;
            }

            this.cats.Add(new FileCategory { Include = false, Code = Convert.ToInt32(this.tcode), Comments = Convert.ToInt32(this.tcomments), ExcludedFiles = this.texcluded, Empty = Convert.ToInt32(this.tempty), FileTypes = "--------", IncludedFiles = this.tincluded, MultilineCommentEnd = "--------", MultilineCommentStart = "--------", Category = "TOTAL", TotalLines = this.tlines, TotalFiles = this.tfiles, SingleLineComment = "--------", NameExclusions = "--------" });

            this.WhatDidWeSkip();
            if (this.tlines == 0 && this.tfiles == 0)
            {
                this.lblFiles.Content = "Nothing scanned...";
                this.lblCode.Content = string.Empty;
                this.lblComments.Content = string.Empty;
                this.lblEmpty.Content = string.Empty;
            }
            else
            {
                this.lblCode.Content = "Code:\t\t" + (this.tcode / this.tlines * 100).ToString("##.#", CultureInfo.CurrentCulture) + "%";
                this.lblComments.Content = "Comments:\t" + (this.tcomments / this.tlines * 100).ToString("##.#", CultureInfo.CurrentCulture) + "%";
                this.lblEmpty.Content = "Empty:\t\t" + (this.tempty / this.tlines * 100).ToString("##.#", CultureInfo.CurrentCulture) + "%";

                this.lblFiles.Content = this.tlines.ToString("### ### ### ###", CultureInfo.CurrentCulture) + " lines, " + this.tfiles.ToString("### ### ### ###", CultureInfo.CurrentCulture) + " files";
            }

            this.dataGridSummary.Items.Refresh();
            this.dgFileCategories.CommitEdit();
            this.dgFileCategories.Items.Refresh();
        }
        
        internal void ExportToCsv()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("File,Lines,Extension,CreationDateTime,Category,Status,Reason,Length,Directory,Parent,CreatedDateTime,LastWriteTime");
            foreach (CsvFile file in this.csvFiles)
            {
                sb.Append(file.File + ",");
                sb.Append(file.Lines + ",");
                sb.Append(file.Extension + ",");
                sb.Append(file.CreatedDateTime + ",");
                sb.Append(file.Category + ",");
                sb.Append(file.Status + ",");
                sb.Append(file.Reason + ",");
                sb.Append(file.Length + ",");
                sb.Append(file.Directory + ",");
                sb.Append(file.Parent + ",");
                sb.Append(file.CreatedDateTime + ",");
                sb.Append(file.LastWriteTime + ",");
                sb.AppendLine();
            }

            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = "csv files (*.csv)|*.csv";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog1.FileName, sb.ToString());
                }
            }
        }

        private void WhatDidWeSkip()
        {
            ArrayList usedExtensions = new ArrayList();
            foreach (FileCategory cat in this.Categories)
            {
                foreach (string s in cat.FileTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!usedExtensions.Contains(s))
                    {
                        usedExtensions.Add(s.ToLower(CultureInfo.CurrentCulture));
                    }
                }
            }

            foreach (FileInfo f in this.foundFiles)
            {
                if (!usedExtensions.Contains(f.Extension.ToLower(CultureInfo.CurrentCulture)))
                {
                    this.ignoredFiles.Add(new IgnoredFile { File = f.FullName, Extension = f.Extension });
                    this.csvFiles.Add(new CsvFile { File = f.FullName, Extension = f.Extension, Status = "Excluded", Reason = "Extension", Lines = 0, CreatedDateTime = f.CreationTime, LastWriteTime = f.LastWriteTime, Length = f.Length, Parent = f.Directory.Parent.Name, Directory = f.Directory.Name });
                }
            }

            this.dataGridIgnoredFiles.ItemsSource = this.ignoredFiles;
            this.dataGridIgnoredFiles.Items.Refresh();

            string[] extension = new string[this.ignoredFiles.Count];
            int j = 0;
            foreach (IgnoredFile ig in this.ignoredFiles)
            {
                extension[j++] = ig.Extension;
            }

            var groups = extension.GroupBy(v => v);
            foreach (var group in groups)
            {
                this.ignoredExtensions.Add(new IgnoredExtension { Extension = group.Key, Count = group.Count() });
            }

            this.dataGridIgnoredExtensions.ItemsSource = this.ignoredExtensions;
            this.dataGridIgnoredExtensions.Items.Refresh();

            this.tabIgnoredFiles.Header = "Ignored Files (" + this.ignoredFiles.Count + ")";
            this.tabIgnoredExtensions.Header = "Ignored Extensions (" + this.ignoredExtensions.Count + ")";
        }

        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "FileTypes")
            {
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }

        private void dataGrid2_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Extension")
            {
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }
        
        private void dataGrid3_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Count")
            {
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }
        
        private void dataGrid4_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Reason")
            {
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }

        private void dgFileCategories_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files1 = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
            if (files1 != null)
            {
                this.OpenJson(files1[0]);
            }
        }

        private void dgFileCategories_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
        }

        private void ProcessPath(FileCategory cat, IEnumerable<FileInfo> fileInfo, ObservableCollection<FileReport> filereport, string fileExclusions, string folderExclusions)
        {
            cat.Code = 0;
            cat.Comments = 0;
            cat.Empty = 0;
            cat.ExcludedFiles = 0;
            cat.IncludedFiles = 0;
            cat.TotalFiles = 0;
            cat.TotalLines = 0;

            if (!cat.Include)
            {
                return;
            }

            foreach (FileInfo f in cat.FileTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).SelectMany(type => fileInfo.Where(f => f.Extension.ToLower(CultureInfo.CurrentCulture) == type.TrimStart().ToLower(CultureInfo.CurrentCulture))))
            {
                cat.TotalFiles++;
                this.CountLines(f, cat, filereport, fileExclusions, folderExclusions);
            }

            cat.Code = cat.TotalLines - cat.Empty - cat.Comments;
        }

        private void CountLines(FileSystemInfo i, FileCategory cat, ObservableCollection<FileReport> filereport, string fileExclusions, string folderExclusions)
        {
            FileInfo thisFile = new FileInfo(i.FullName);
            if (!string.IsNullOrEmpty(fileExclusions))
            {
                if (fileExclusions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Any(s => thisFile.Name.ToLower(CultureInfo.CurrentCulture).Contains(s.ToLower(CultureInfo.CurrentCulture))))
                {
                    filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Global File Name", Status = "Excluded", Lines = 0, Category = cat.Category });
                    this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Global File Name", Status = "Excluded", Lines = 0, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
                    cat.ExcludedFiles++;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(folderExclusions))
            {
                if (folderExclusions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Any(s => thisFile.DirectoryName.ToLower(CultureInfo.CurrentCulture).Contains(s.ToLower(CultureInfo.CurrentCulture))))
                {
                    filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Global Folder Name", Status = "Excluded", Lines = 0, Category = cat.Category });
                    this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Global Folder Name", Status = "Excluded", Lines = 0, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
                    cat.ExcludedFiles++;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(cat.NameExclusions))
            {
                if (cat.NameExclusions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Any(s => thisFile.Name.ToLower(CultureInfo.CurrentCulture).Contains(s.ToLower(CultureInfo.CurrentCulture))))
                {
                    filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Category File Name", Status = "Excluded", Lines = 0, Category = cat.Category });
                    this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Category File Name", Status = "Excluded", Lines = 0, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
                    cat.ExcludedFiles++;
                    return;
                }
            }

            if (Math.Abs(this.largerThan) > 0 && (thisFile.Length > this.largerThan * 1024 * 1024))
            {
                filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Large Size", Status = "Excluded", Lines = 0, Category = cat.Category });
                this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Large Size", Status = "Excluded", Lines = 0, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
                cat.ExcludedFiles++;
                return;
            }

            if (Math.Abs(this.smallerThan) > 0 && (thisFile.Length < this.smallerThan * 1024))
            {
                filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Small Size", Status = "Excluded", Lines = 0, Category = cat.Category });
                this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Small Size", Status = "Excluded", Lines = 0, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
                cat.ExcludedFiles++;
                return;
            }

            bool incomment = false;
            bool handlemulti = !string.IsNullOrWhiteSpace(cat.MultilineCommentStart);
            int filelinecount = 0;
            foreach (string line in File.ReadLines(i.FullName))
            {
                filelinecount++;
                string line1 = line;
                if (string.IsNullOrWhiteSpace(line))
                {
                    cat.Empty++;
                }
                else
                {
                    if (handlemulti)
                    {
                        if (incomment)
                        {
                            cat.Comments++;
                            if (line1.TrimEnd(' ').EndsWith(cat.MultilineCommentEnd, StringComparison.OrdinalIgnoreCase))
                            {
                                incomment = false;
                            }
                        }
                        else
                        {
                            if (line1.TrimStart(' ').StartsWith(cat.MultilineCommentStart, StringComparison.OrdinalIgnoreCase))
                            {
                                incomment = true;
                                cat.Comments++;
                                if (line1.TrimEnd(' ').EndsWith(cat.MultilineCommentEnd, StringComparison.OrdinalIgnoreCase))
                                {
                                    incomment = false;
                                }
                            }
                            else
                            {
                                foreach (string s in cat.SingleLineComment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(s => line1.TrimStart(' ').StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                                {
                                    cat.Comments++;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string s in cat.SingleLineComment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(s => line1.TrimStart(' ').StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                        {
                            cat.Comments++;
                        }
                    }
                }

                cat.TotalLines++;
            }

            filereport.Add(new FileReport { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Match", Status = "Included", Lines = filelinecount, Category = cat.Category });
            this.csvFiles.Add(new CsvFile { File = thisFile.FullName, Extension = thisFile.Extension, Reason = "Match", Status = "Included", Lines = filelinecount, Category = cat.Category, CreatedDateTime = thisFile.CreationTime, LastWriteTime = thisFile.LastWriteTime, Length = thisFile.Length, Parent = thisFile.Directory.Parent.Name, Directory = thisFile.Directory.Name });
            cat.IncludedFiles++;
        }

        private void LineCounter_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.parentWindow = (MainWindow)Window.GetWindow(this);
        }
    }
}
