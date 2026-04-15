namespace Logging
{
    public class ApplicationIds
    {
        public ApplicationIds(string logosStorageId, string gethId, string prometheusId, string logosStorageContractsId, string grafanaId)
        {
            LogosStorageId = logosStorageId;
            GethId = gethId;
            PrometheusId = prometheusId;
            LogosStorageContractsId = logosStorageContractsId;
            GrafanaId = grafanaId;
        }

        public string LogosStorageId { get; }
        public string GethId { get; }
        public string PrometheusId { get; }
        public string LogosStorageContractsId { get; }
        public string GrafanaId { get; }
    }
}
