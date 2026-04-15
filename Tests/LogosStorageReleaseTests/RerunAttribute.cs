using NUnit.Framework;

namespace LogosStorageReleaseTests
{
    public class RerunAttribute : ValuesAttribute
    {
        private const int NumberOfReRuns = 8;

        public RerunAttribute()
        {
            var list = new List<object>();
            for (var i = 0; i < NumberOfReRuns; i++) list.Add(i);
            data = list.ToArray();
        }
    }
}
