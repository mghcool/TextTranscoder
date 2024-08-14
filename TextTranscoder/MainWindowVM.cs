using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UtfUnknown;

namespace TextTranscoder
{
    public partial class MainWindowVM : ObservableObject
    {
        public string[] InputEncodingList { get; } = ["自动识别", "系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 (BOM)", "GBK"];
        public string[] OutputEncodingList { get; } = ["系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 (BOM)", "GBK"];

        [ObservableProperty]
        private int _selectedInputEncodingIndex = 0;

        [ObservableProperty]
        private int _selectedOutputEncodingIndex = 0;

        /// <summary>系统默认编码</summary>
        [ObservableProperty]
        private string _systemDefaultEncoding = string.Empty;

        /// <summary>输入路径列表</summary>
        public ObservableCollection<InputPathInfo> InputPathList { get; } = new ();

        /// <summary>选中的列表项索引</summary>
        [ObservableProperty]
        private int _selectedInputPathListIndex = -1;

        /// <summary>输出路径</summary>
        [ObservableProperty]
        private string _outputDirectory = string.Empty;

        /// <summary>覆盖源文件</summary>
        [ObservableProperty]
        private bool _overwriteSourceFile = true;

        /// <summary>启用文件夹递归遍历</summary>
        [ObservableProperty]
        private bool _folderRecursiveTraversal = false;

        /// <summary>表示正在转码中</summary>
        [ObservableProperty]
        private bool _isTranscoding = false;

        public MainWindowVM()
        {
            SystemDefaultEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage).HeaderName.ToUpper();
        }

        /// <summary>
        /// 添加文件
        /// </summary>
        [RelayCommand]
        private void AddFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "所有文件 (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    InputPathList.Add(new InputPathInfo(false, filePath, false));
                }
            }
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        [RelayCommand]
        private void AddFolder()
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Multiselect = true;
            if (openFolderDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFolderDialog.FolderNames)
                {
                    InputPathList.Add(new InputPathInfo(true, filePath, false));
                }
            }
        }

        /// <summary>
        /// 移除选中项
        /// </summary>
        [RelayCommand]
        private void RemoveSelected()
        {
            InputPathList.RemoveAt(SelectedInputPathListIndex);
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        [RelayCommand]
        private void ClearList()
        {
            InputPathList.Clear();
        }

        /// <summary>
        /// 浏览输出目录
        /// </summary>
        [RelayCommand]
        private void BrowseOutputDirectory()
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() == true)
            {
                OutputDirectory = openFolderDialog.FolderName;
            }
        }

        /// <summary>
        /// 开始转码
        /// </summary>
        [RelayCommand]
        private void StartTranscode()
        {
            IsTranscoding = true;
            foreach (InputPathInfo inputPathInfo in InputPathList)
            {
                if (inputPathInfo.IsDirectory)
                    TranscodeFolder(inputPathInfo.Path);
                else
                    TranscodeFile(inputPathInfo.Path);
                inputPathInfo.IsComplete = true;
            }
            IsTranscoding = false;
        }

        /// <summary>
        /// 转码文件
        /// </summary>
        /// <param name="filePath"></param>
        private void TranscodeFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists || fileInfo.Length > (10 * 1024 * 1024)) return;

            using FileStream fileStream = File.OpenRead(filePath);
            DetectionResult result = CharsetDetector.DetectFromStream(fileStream);
            Encoding sourceEncoding = result.Detected.Encoding;
            Encoding targetEncoding = Encoding.UTF8;

            if(sourceEncoding.CodePage == targetEncoding.CodePage) return;

            //new UTF8Encoding(true);
            //new UnicodeEncoding(true, false);
            //new UTF32Encoding(true, true);

            fileStream.Position = 0;
            using StreamReader reader = new StreamReader(fileStream, sourceEncoding);
            string text = reader.ReadToEnd();
            reader.Close();
            using StreamWriter writer = new StreamWriter(filePath, false, targetEncoding);
            writer.Write(text);
            writer.Close();
        }

        private void TranscodeFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                TranscodeFile(file);
            }

            if(!FolderRecursiveTraversal) return;
            string[] folders = Directory.GetDirectories(folderPath);
            foreach (string folder in folders)
            {
                TranscodeFolder(folder);
            }
        }
    }
}
