using System.Globalization;
using CsvHelper.Configuration;

namespace log4net.tools.integration
{
    internal class MetricsMap : ClassMap<LatencyWithContext>
    {
        public MetricsMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.LatencyUs).TypeConverterOption.Format("0.00");
            Map(m => m.DateTime).TypeConverterOption.Format("s");
        }
    }
}
