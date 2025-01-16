using System.Globalization;
using CsvHelper;
using log4net.Core;

namespace log4net.tools.integration
{
    public class MetricsCsvWriter: IMetricsWriter, IOptionHandler
    {
        public string CsvFilePath { get; set; }

        public int MaxRowCount { get; set; } = 1000000;

        private static readonly object Lock = new object(); //todo: use dict of locks instead
        private int _rowCount;

        public void WriteLatency(LatencyWithContext latency)
        {
            if (latency == null) throw new ArgumentNullException(nameof(latency));

            if (_rowCount >= MaxRowCount)
            {
                return;
            }

            lock (Lock)
            {
                if (_rowCount >= MaxRowCount)
                {
                    return;
                }

                using (var writer = new StreamWriter(CsvFilePath, true)) //todo: consider allocation in the ctor and dispose in the this.Dispose
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<MetricsMap>();
                    csv.WriteRecord(latency);
                    csv.NextRecord();
                }

                _rowCount++;
            }
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrWhiteSpace(CsvFilePath))
            {
                throw new ArgumentNullException(nameof(CsvFilePath));
            }

            lock (Lock)
            {
                if (!File.Exists(CsvFilePath))
                {
                    using (var writer = new StreamWriter(CsvFilePath))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteHeader<LatencyWithContext>();
                        csv.NextRecord();
                    }
                    
                    _rowCount = 1;
                }
                else
                {
                    using (StreamReader file = new StreamReader(CsvFilePath))
                    {
                        while (file.ReadLine() != null)
                        {
                            _rowCount++;
                        }
                    }
                }
            }
        }
    }
}
