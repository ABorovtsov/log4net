using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace log4net.tools.benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(new Job
                {
                    Run = { LaunchCount = 1, WarmupCount = 2, IterationCount = 30 }
                })
                .AddColumn(new[] { StatisticColumn.Min, StatisticColumn.Max })
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Millisecond))
                .AddExporter(new CsvExporter(CsvSeparator.CurrentCulture,
                    SummaryStyle.Default.WithTimeUnit(TimeUnit.Millisecond)));

            BenchmarkRunner.Run(typeof(Program).Assembly, config);
        }
    }
}
