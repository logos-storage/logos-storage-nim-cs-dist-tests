namespace StoragePlugin
{
    public class LogosStorageDockerImage
    {
        private const string DefaultDockerImage = "logosstorage/logos-storage-nim:latest-dist-tests";

        public static string Override { get; set; } = string.Empty;

        public string GetLogosStorageDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("STORAGEDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            if (!string.IsNullOrEmpty(Override)) return Override;
            return DefaultDockerImage;
        }
    }
}
