namespace StoragePlugin
{
    public class LogosStorageExePath
    {
        private readonly string[] paths = [
            Path.Combine("d:", "Dev", "logos-storage-nim", "build", "storage.exe"),
            Path.Combine("c:", "Projects", "logos-storage-nim", "build", "storage.exe")
        ];

        private string selectedPath = string.Empty;

        public LogosStorageExePath()
        {
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    selectedPath = p;
                    return;
                }
            }
        }

        public string Get()
        {
            return selectedPath;
        }
    }
}
