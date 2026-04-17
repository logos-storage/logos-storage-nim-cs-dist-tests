using Logging;

namespace LogosStorageClient
{
    public interface IProcessControlFactory
    {
        IProcessControl CreateProcessControl(ILogosStorageInstance instance);
    }

    public interface IProcessControl
    {
        void Stop(bool waitTillStopped);
        IDownloadedLog DownloadLog(LogFile file);
        void DeleteDataDirFolder();
        bool HasCrashed();
    }

    public class DoNothingProcessControlFactory : IProcessControlFactory
    {
        public IProcessControl CreateProcessControl(ILogosStorageInstance instance)
        {
            return new DoNothingProcessControl();
        }
    }

    public class DoNothingProcessControl : IProcessControl
    {
        public void DeleteDataDirFolder()
        {
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            throw new NotImplementedException("Not supported by DoNothingProcessControl");
        }

        public bool HasCrashed()
        {
            return false;
        }

        public void Stop(bool waitTillStopped)
        {
        }
    }
}
