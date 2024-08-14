using System.Collections.ObjectModel;
using System.Diagnostics;
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
        /// <summary></summary>
        public string[] InputEncodingList { get; } = ["自动识别", "系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 (BOM)", "GBK"];

        /// <summary></summary>
        public string[] OutputEncodingList { get; } = ["系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 (BOM)", "UTF32", "UTF32 (BOM)", "GBK"];

        /// <summary></summary>
        public string[] FilterModeList { get; } = ["排除", "包含"];

        /// <summary></summary>
        [ObservableProperty]
        private int _selectedInputEncodingIndex = 0;

        /// <summary></summary>
        [ObservableProperty]
        private int _selectedOutputEncodingIndex = 0;

        /// <summary>过滤模式的选择</summary>
        [ObservableProperty]
        private int _selectedFilterModeIndex = 0;

        /// <summary>系统默认编码</summary>
        private readonly Encoding SystemDefaultEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

        /// <summary>系统默认编码名</summary>
        [ObservableProperty]
        private string _systemDefaultEncodingName = string.Empty;

        /// <summary>过滤列表</summary>
        public ObservableCollection<string> FilterLists { get; } = new();

        /// <summary>要添加的过滤标签</summary>
        [ObservableProperty]
        private string _addFilterTagText = string.Empty;

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
            SystemDefaultEncodingName = SystemDefaultEncoding.HeaderName.ToUpper();
        }

        /// <summary>
        /// 添加过滤标签
        /// </summary>
        [RelayCommand]
        private void AddFilterTag()
        {
            string filterText = AddFilterTagText.Trim();
            if(!string.IsNullOrWhiteSpace(filterText) && !FilterLists.Contains(filterText))
            {
                FilterLists.Add(filterText);
            }
            AddFilterTagText = string.Empty;
        }

        /// <summary>
        /// 清空过滤标签
        /// </summary>
        [RelayCommand]
        private void ClearFilterTag()
        {
           FilterLists.Clear();
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
            if(SelectedInputPathListIndex > 0)
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
            Encoding outEncoding;
            if (SelectedOutputEncodingIndex == 0)
                outEncoding = SystemDefaultEncoding;
            else if (SelectedOutputEncodingIndex == 1)
                outEncoding = new UTF8Encoding(false);
            else if (SelectedOutputEncodingIndex == 2)
                outEncoding = new UTF8Encoding(true);
            else if (SelectedOutputEncodingIndex == 3)
                outEncoding = new UnicodeEncoding(true, false);
            else if (SelectedOutputEncodingIndex == 4)
                outEncoding = new UnicodeEncoding(true, true);
            else if (SelectedOutputEncodingIndex == 5)
                outEncoding = new UTF32Encoding(true, false);
            else if (SelectedOutputEncodingIndex == 6)
                outEncoding = new UTF32Encoding(true, true);
            else
                outEncoding = Encoding.GetEncoding(OutputEncodingList[SelectedOutputEncodingIndex]);
            foreach (InputPathInfo inputPathInfo in InputPathList)
            {
                if (inputPathInfo.IsDirectory)
                    TranscodeFolder(inputPathInfo.Path, outEncoding);
                else
                    TranscodeFile(inputPathInfo.Path, outEncoding);
                inputPathInfo.IsComplete = true;
            }
            IsTranscoding = false;
        }

        /// <summary>
        /// 转码文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="targetEncoding"></param>
        private void TranscodeFile(string filePath, Encoding targetEncoding)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Length > (10 * 1024 * 1024)) return;

            using FileStream fileStream = File.OpenRead(filePath);
            DetectionResult result = CharsetDetector.DetectFromStream(fileStream);
            Encoding sourceEncoding = result.Detected.Encoding;
            if(result.Detected.Confidence < 0.5) Debug.WriteLine($"Confidence: {result.Detected.Confidence}");
            bool hasBOM = targetEncoding.GetPreamble().Length > 0;
            if(sourceEncoding.CodePage == targetEncoding.CodePage && hasBOM == result.Detected.HasBOM) return;
            Debug.WriteLine($"{sourceEncoding.HeaderName} >> {targetEncoding.HeaderName} {result.Detected.Confidence:P0}\t{filePath}");

            fileStream.Position = 0;
            using StreamReader reader = new StreamReader(fileStream, sourceEncoding);
            string text = reader.ReadToEnd();
            reader.Close();
            using StreamWriter writer = new StreamWriter(filePath, false, targetEncoding);
            writer.Write(text);
            writer.Close();
        }

        /// <summary>
        /// 将文件夹下的所有文件转码
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="targetEncoding"></param>
        private void TranscodeFolder(string folderPath, Encoding targetEncoding)
        {
            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                TranscodeFile(file, targetEncoding);
            }

            if(!FolderRecursiveTraversal) return;
            string[] folders = Directory.GetDirectories(folderPath);
            foreach (string folder in folders)
            {
                TranscodeFolder(folder, targetEncoding);
            }
        }
    }
}
