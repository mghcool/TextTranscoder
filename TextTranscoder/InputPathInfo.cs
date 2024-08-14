using CommunityToolkit.Mvvm.ComponentModel;

namespace TextTranscoder
{
    /// <summary>
    /// 输入路径信息
    /// </summary>
    public partial class InputPathInfo : ObservableObject
    {
        /// <summary>是否为目录</summary>
        [ObservableProperty]
        private bool _isDirectory;

        /// <summary>路径</summary>
        [ObservableProperty]
        private string _path = string.Empty;

        /// <summary>是否转码完成</summary>
        [ObservableProperty]
        private bool _isComplete;

        public InputPathInfo() { }

        /// <summary>
        /// 实例化一个输入路径信息
        /// </summary>
        /// <param name="isDirectory">是否为目录</param>
        /// <param name="path">路径</param>
        /// <param name="isComplete">是否转码完成</param>
        public InputPathInfo(bool isDirectory, string path, bool isComplete)
        {
            IsDirectory = isDirectory;
            Path = path;
            IsComplete = isComplete;
        }
    }
}
