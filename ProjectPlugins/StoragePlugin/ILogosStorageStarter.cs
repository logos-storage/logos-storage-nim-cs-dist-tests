using LogosStorageClient;

namespace StoragePlugin
{
    public interface ILogosStorageStarter
    {
        ILogosStorageInstance[] BringOnline(LogosStorageSetup logosStorageSetup);
        void Decommission();
    }
}
