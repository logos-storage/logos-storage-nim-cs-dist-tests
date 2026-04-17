using LogosStorageClient.Hooks;
using FileUtils;
using Logging;
using WebUtils;

namespace LogosStorageClient
{
    public class StorageNodeFactory
    {
        private readonly ILog log;
        private readonly IFileManager fileManager;
        private readonly LogosStorageHooksFactory hooksFactory;
        private readonly IHttpFactory httpFactory;
        private readonly IProcessControlFactory processControlFactory;

        public StorageNodeFactory(ILog log, IFileManager fileManager, LogosStorageHooksFactory hooksFactory, IHttpFactory httpFactory, IProcessControlFactory processControlFactory)
        {
            this.log = log;
            this.fileManager = fileManager;
            this.hooksFactory = hooksFactory;
            this.httpFactory = httpFactory;
            this.processControlFactory = processControlFactory;
        }

        public StorageNodeFactory(ILog log, HttpFactory httpFactory, string dataDir)
            : this(log, new FileManager(log, dataDir), new LogosStorageHooksFactory(), httpFactory, new DoNothingProcessControlFactory())
        {
        }

        public StorageNodeFactory(ILog log, string dataDir)
            : this(log, new HttpFactory(log), dataDir)
        {
        }

        public IStorageNode CreateStorageNode(ILogosStorageInstance instance)
        {
            var processControl = processControlFactory.CreateProcessControl(instance);
            var access = new LogosStorageAccess(log, httpFactory, processControl, instance);
            var hooks = hooksFactory.CreateHooks(access.GetName());
            var node =  new StorageNode(log, access, fileManager, hooks);
            node.Initialize();
            return node;
        }

    }
}
