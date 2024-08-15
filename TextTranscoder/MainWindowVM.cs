using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Win32;
using UtfUnknown;

namespace TextTranscoder
{
    public partial class MainWindowVM : ObservableObject
    {
        /// <summary>输入编码列表</summary>
        public string[] InputEncodingList { get; } = ["自动识别", "系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 Big", "UTF32", "UTF32 Big", "GBK", "Shift JIS", "EUC-KR"];

        /// <summary>输出编码列表</summary>
        public string[] OutputEncodingList { get; } = ["系统默认", "UTF8", "UTF8 (BOM)", "UTF16", "UTF16 Big", "UTF32", "UTF32 Big", "GBK", "Shift JIS", "EUC-KR"];

        /// <summary>过滤模式列表</summary>
        public string[] FilterModeList { get; } = ["排除", "包含"];

        /// <summary>输入编码选中索引</summary>
        [ObservableProperty]
        private int _selectedInputEncodingIndex = 0;

        /// <summary>输出编码选中索引</summary>
        [ObservableProperty]
        private int _selectedOutputEncodingIndex = 1;

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

        /// <summary>转码中对话框日志</summary>
        [ObservableProperty]
        private string _transcodeingDialogLog = string.Empty;

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
        /// 文件拖放操作
        /// </summary>
        [RelayCommand]
        private void ListViewDrop(DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    InputPathList.Add(new InputPathInfo(true, path, false));
                }
                else
                {
                    InputPathList.Add(new InputPathInfo(false, path, false));
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
        /// 转码历史
        /// </summary>
        [RelayCommand]
        private void TranscodingHistory()
        {
            Dialog dialog = Dialog.Show<TranscodeingDialog>();
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
        /// 输出目录拖放操作
        /// </summary>
        /// <param name="e"></param>
        [RelayCommand]
        private void OutputDirectoryDrop(DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    OutputDirectory = path;
                    break;
                }
            }
        }

        /// <summary>
        /// 开始转码
        /// </summary>
        [RelayCommand]
        private async Task StartTranscode()
        {
            if (InputPathList.Count == 0) return;
            if (!OverwriteSourceFile && string.IsNullOrWhiteSpace(OutputDirectory))
            {
                Growl.Warning(new ()
                {
                    WaitTime = 2,
                    Message = "请选择输出目录",
                    ShowDateTime = true
                });
                return;
            }
            LogClear();
            Dialog dialog = Dialog.Show<TranscodeingDialog>();
            IsTranscoding = true;
            Encoding outEncoding;
            if (SelectedOutputEncodingIndex == 0)
                outEncoding = SystemDefaultEncoding;
            else if (SelectedOutputEncodingIndex == 1)
                outEncoding = new UTF8Encoding(false);
            else if (SelectedOutputEncodingIndex == 2)
                outEncoding = new UTF8Encoding(true);
            else if (SelectedOutputEncodingIndex == 3)
                outEncoding = new UnicodeEncoding(false, false);
            else if (SelectedOutputEncodingIndex == 4)
                outEncoding = new UnicodeEncoding(true, false);
            else if (SelectedOutputEncodingIndex == 5)
                outEncoding = new UTF32Encoding(false, false);
            else if (SelectedOutputEncodingIndex == 6)
                outEncoding = new UTF32Encoding(true, false);
            else
                outEncoding = Encoding.GetEncoding(OutputEncodingList[SelectedOutputEncodingIndex]);
            await Task.Run(() =>
            {
                foreach (InputPathInfo inputPathInfo in InputPathList)
                {
                    if (inputPathInfo.IsDirectory)
                    {
                        TranscodeFolder(inputPathInfo.Path, outEncoding);
                        inputPathInfo.IsComplete = true;
                    }
                    else
                    {
                        inputPathInfo.IsComplete = TranscodeFile(inputPathInfo.Path, outEncoding);
                    }
                }
            });
            IsTranscoding = false;
        }

        /// <summary>
        /// 转码文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="targetEncoding"></param>
        private bool TranscodeFile(string filePath, Encoding targetEncoding)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Length > (10 * 1024 * 1024))
            {
                LogOut($"文件大于10MB，跳过。 {filePath}");
                return false;
            }
            using FileStream fileStream = File.OpenRead(filePath);
            DetectionDetail? detected = CharsetDetector.DetectFromStream(fileStream).Detected;
            if(detected == null)
            {
                LogOut($"文件可能为非文本，跳过。 {filePath}");
                return false;
            }
            
            if (detected.Confidence < 0.5)
            {
                LogOut($"识别度低于50%，{detected.Confidence:P0}，跳过。 {filePath}");
                return false;
            }
            Encoding sourceEncoding = detected.Encoding;
            bool hasBOM = targetEncoding.GetPreamble().Length > 0;
            if(sourceEncoding.CodePage == targetEncoding.CodePage && hasBOM == detected.HasBOM)
            {
                LogOut($"编码相同，跳过。 {filePath}");
                return true;
            }
            fileStream.Position = 0;
            using StreamReader reader = new StreamReader(fileStream, sourceEncoding);
            string text = reader.ReadToEnd();
            reader.Close();

            string outputFilePath = filePath;
            if (!OverwriteSourceFile) outputFilePath = Path.Combine(OutputDirectory, fileInfo.Name);
            using StreamWriter writer = new StreamWriter(outputFilePath, false, targetEncoding);
            writer.Write(text);
            writer.Close();
            LogOut($"原始编码：{sourceEncoding.HeaderName}，识别度：{detected.Confidence:P0}。 {filePath}");
            return true;
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

        /// <summary>
        /// 输出日志
        /// </summary>
        private void LogOut(string msg) => TranscodeingDialogLog += $"{DateTime.Now:HH:mm:ss.fff} | {msg}{Environment.NewLine}";

        /// <summary>
        /// 清理日志
        /// </summary>
        private void LogClear() => TranscodeingDialogLog = string.Empty;
    }
}
