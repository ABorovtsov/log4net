using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using log4net.Core;

namespace log4net.tools.integration
{
    public class MetricsCsvWriter: IMetricsWriter, IOptionHandler
    {
        public string CsvFilePath { get; set; }

        private static readonly object Lock = new object(); //todo: use dict of locks instead

        public void WriteLatency(LatencyWithContext latency)
        {
            if (latency == null) throw new ArgumentNullException(nameof(latency));

            lock (Lock)
            {
                using (var writer = new StreamWriter(CsvFilePath, true)) //todo: consider allocation in the ctor and dispose in the this.Dispose
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<MetricsMap>();
                    csv.WriteRecord(latency);
                    csv.NextRecord();
                }
            }
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrWhiteSpace(CsvFilePath))
            {
                throw new ArgumentNullException(nameof(CsvFilePath));
            }

            if (!File.Exists(CsvFilePath))
            {
                lock (Lock)
                {
                    if (!File.Exists(CsvFilePath))
                    {
                        using (var writer = new StreamWriter(CsvFilePath))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            csv. WriteHeader<LatencyWithContext>();
                            csv.NextRecord();
                        }
                    }
                }
            }
        }
    }
}
