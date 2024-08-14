namespace TextTranscoder
{
    /// <summary>
    /// 输入路径信息
    /// </summary>
    public class InputPathInfo
    {
        /// <summary>是否为目录</summary>
        public bool IsDirectory { get; set; }

        /// <summary>路径</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>是否转码完成</summary>
        public bool IsComplete { get; set; }

        public InputPathInfo() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isDirectory"></param>
        /// <param name="path"></param>
        /// <param name="isComplete"></param>
        public InputPathInfo(bool isDirectory, string path, bool isComplete)
        {
            IsDirectory = isDirectory;
            Path = path;
            IsComplete = isComplete;
        }
    }
}
