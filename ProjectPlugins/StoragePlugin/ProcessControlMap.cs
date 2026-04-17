using LogosStorageClient;

namespace StoragePlugin
{
    public class ProcessControlMap : IProcessControlFactory
    {
        private readonly Dictionary<string, IProcessControl> processControlMap = new Dictionary<string, IProcessControl>();

        public void Add(ILogosStorageInstance instance, IProcessControl control)
        {
            processControlMap.Add(instance.Name, control);
        }

        public void Remove(ILogosStorageInstance instance)
        {
            processControlMap.Remove(instance.Name);
        }

        public IProcessControl CreateProcessControl(ILogosStorageInstance instance)
        {
            return Get(instance);
        }

        public IProcessControl Get(ILogosStorageInstance instance)
        {
            return processControlMap[instance.Name];
        }

        public void StopAll()
        {
            var pcs = processControlMap.Values.ToArray();
            processControlMap.Clear();

            foreach (var c in pcs) c.Stop(waitTillStopped: true);
        }
    }
}
