# About log4net Tools
[<img align="right" width="100px" src="https://github.com/ABorovtsov/log4net/blob/main/img/icon.png?raw=true" />](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools)

The project was designed to supplement the log4net base functionality with often demanded structure blocks like appenders, log parsers and handlers.

**Latest Builds**:

- log4net.tools <a href="https://www.nuget.org/packages/log4net.tools"><img src="https://img.shields.io/nuget/v/log4net.tools.svg?style=flat&logo=nuget"></a> 
- log4net.tools.integration <a href="https://www.nuget.org/packages/log4net.tools.integration"><img src="https://img.shields.io/nuget/v/log4net.tools.integration.svg?style=flat&logo=nuget"></a> 
<br/>

## [ForwardingAppenderAsync](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/ForwardingAppenderAsync.cs)

The appender wrapps any log4net appender putting the async buffer in front. The independent background worker is responsible for dequeuing of items from the buffer without blocking a client app. 

![Functional blocks](https://raw.githubusercontent.com/ABorovtsov/log4net/main/img/ForwardingAppenderAsync.png?raw=true)

Thus waiting needing before for example to write a log in a database is delegated now and the only place where the client app is blocking is the stage of the in-memory enqueuing.

[<img width="480px" src="https://raw.githubusercontent.com/ABorovtsov/log4net/main/img/metrics/enqueue_dequeue.png" />](https://github.com/ABorovtsov/log4net/blob/main/log_analyzer/appender_metrics.ipynb)

The 'Dequeue' graph reflects the latency in microseconds of the RollingFileAppender (taken just as example) which works under the hood as the attached synchronous appender. A consumer is blocked only during the 'Enqueue' microseconds.
<br/>

The approach allows:
- to get the minimal blocking of the client app;
- to turn any 'old' synchronous appender into the 'async' version easily without additional coding or recompile;
- to eliminate concurrent waits on the [internal lock section](https://git-wip-us.apache.org/repos/asf?p=logging-log4net.git;a=blob;f=src/log4net/Appender/AppenderSkeleton.cs;h=44b68c7555944ddcc2e862901ce8513ce0bff10f;hb=refs/heads/master#l297) as the background worker is singlethreaded;
- to build aggregations over the buffer. For example, to drop log duplicates or generate stats on the fly.
<br/>

Configurable logic:
- [handling the buffer overflow situation](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/BufferOverflowBehaviour.cs)
- [closing behavior](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/BufferClosingType.cs)
<br/>

### XML Configuration
The example of the minimal configuration:
```xml
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <appender-ref ref="RollingFileAppender" />
</appender>
```

The example of the advanced configuration:
```xml
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <BufferSize value="1000"/>
    <Fix value="260"/>
    <BufferOverflowBehaviour value="RejectNew"/>
    <BufferClosingType value="DumpToLog"/>

    <appender-ref ref="DebugAppender" />
    <appender-ref ref="RollingFileAppender" />
    <appender-ref ref="AdoNetAppender" />
</appender>
```
More examples of the xml configuration are available [here](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools.benchmarks/App.config).

### [RollingFileAppender Benchmark](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools.benchmarks/RollingFileAppenderTest.cs)

#### 10K of sequential info-logs:

|                        Method |      Mean |
|------------------------------ |----------:|
| RollingFileAppender           | 1,742.69 ms |
| Buffered RollingFileAppender  | 180.728 ms |
| **Forwarded** RollingFileAppender |5.43 ms|

#### 10K of "parallel" (Parallel.For) info-logs:

|                        Method |      Mean |
|------------------------------ |----------:|
| RollingFileAppender           | 1,668.64 ms |
| Buffered RollingFileAppender  | 254.72 ms |
| **Forwarded** RollingFileAppender |   4.45 ms |

### [AdoNetAppender Benchmark](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools.benchmarks/AdoNetAppenderTest.cs)

#### 10K of sequential info-logs:

|                        Method |      Mean |
|------------------------------ |----------:|
| AdoNetAppender           | 3,336.11 ms |
| **Forwarded** AdoNetAppender |5.54 ms|

#### 10K of "parallel" (Parallel.For) info-logs:

|                        Method |      Mean |
|------------------------------ |----------:|
| AdoNetAppender           | 2,797.39 ms |
| **Forwarded** AdoNetAppender |   4.62 ms |

### Worker Pool
The worker pool allows to improve throughput between the buffer and the attached appenders as it allows to dequeue the cached logs in parallel.

For example, the test with the RollingFileAppender shows the difference in intencity of 'dequeue' events:
![1vs5_workers_rps](https://github.com/ABorovtsov/log4net/blob/main/img/metrics/1vs5_workers_rps.png?raw=true)

The latency is on the same level. We observe that more workers generate more dots in the stats.
![1vs5_workers_latency](https://github.com/ABorovtsov/log4net/blob/main/img/metrics/1vs5_workers_latency.png?raw=true)

The latency is on the same level. We observe that more workers generate more dots in the stats.
![1vs5_workers_latency](https://github.com/ABorovtsov/log4net/blob/main/img/metrics/1vs5_workers_latency.png?raw=true)

The buffer size growth goes not as steep as with the single threaded configuration as workers more productive in processing of the benchmark stress load.
![1vs5_workers_buffer](https://github.com/ABorovtsov/log4net/blob/main/img/metrics/1vs5_workers_buffer.png?raw=true)

## [ForwardingAppenderAsyncWithMetrics](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/Metrics/ForwardingAppenderAsyncWithMetrics.cs)
The appender grabs the metrics: LatencyUs, BufferSize, AllocatedBytes in addition to the [ForwardingAppenderAsync](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/ForwardingAppenderAsync.cs) functionality. The default output is Trace Info. Metrics bring some additional load so it is recommended to use the ForwardingAppenderAsync in scenarios where the minimum latency is required.

The example of the minimal xml configuration:
```xml
<appender name="Forwarding2RollingFileAppenderWithMetrics" type="log4net.tools.ForwardingAppenderAsyncWithMetrics, log4net.tools">
    <appender-ref ref="RollingFileAppender" />
</appender>
```
The example above uses "Trace" channel to output the metrics.

The example of the advanced xml configuration:
```xml
<appender name="Forwarding2RollingFileAppenderWithMetrics" type="log4net.tools.ForwardingAppenderAsyncWithMetrics, log4net.tools">
    <MetricsWriter type="log4net.tools.integration.MetricsCsvWriter, log4net.tools.integration">
        <CsvFilePath value="data.csv"/>
        <MaxRowCount value="100000"/>
    </MetricsWriter> 

    <BufferSize value="1000"/>
    <Fix value="260"/>
    <BufferOverflowBehaviour value="RejectNew"/>
    <BufferClosingType value="DumpToLog"/>

    <appender-ref ref="DebugAppender" />
    <appender-ref ref="RollingFileAppender" />
    <appender-ref ref="AdoNetAppender" />
</appender>
```
MetricsCsvWriter is available in the [log4net.tools.integration](https://www.nuget.org/packages/log4net.tools.integration) package.

The example of the output:
```csv
DateTime            ,LatencyUs ,BufferSize ,CallerName ,AllocatedBytes
2021-05-04T11:49:22 ,2.00      ,76         ,DoAppend   ,5491168
2021-05-04T11:49:22 ,2.80      ,77         ,DoAppend   ,5587296
2021-05-04T11:49:22 ,318.80    ,77         ,Dequeue    ,5684840
2021-05-04T11:49:22 ,2.40      ,77         ,DoAppend   ,1269424
2021-05-04T11:49:22 ,2.50      ,78         ,DoAppend   ,1367776
2021-05-04T11:49:22 ,2.00      ,79         ,DoAppend   ,1466128
2021-05-04T11:49:22 ,2.30      ,80         ,DoAppend   ,1564480
2021-05-04T11:49:22 ,1.50      ,81         ,DoAppend   ,1662832
2021-05-04T11:49:22 ,273.80    ,81         ,Dequeue    ,1752992
```
The python [notebook](https://github.com/ABorovtsov/log4net/blob/main/log_analyzer/appender_metrics.ipynb) to analyze the metrics.

## [Log Analyzer](https://github.com/ABorovtsov/log4net/blob/main/log_analyzer/simple_log_parser.py)
It's the python script which parses log4net logs and returns the stats related to the log levels and error messages.
```python
import pprint
from colorama import Fore, init
from simple_log_parser import SimpleLogParser

error_stats = SimpleLogParser('./log.txt').errors_count(from_date='2021-04-15')

init(autoreset=True)
print(Fore.GREEN + 'Error Levels:')
pprint.pprint(error_stats[0])
print(Fore.GREEN + 'Error Messages:')
pprint.pprint(error_stats[1])
```
The code above prints this:
```
Error Levels:
defaultdict(<class 'int'>, {'ERROR': 6})

Error Messages:
defaultdict(<class 'int'>,
            {'Microsoft.AspNetCore.Connections.ConnectionResetException: The client has disconnected\n': 1,
             "System.ArgumentException: 'A' is invalid": 1,
             "System.ArgumentException: 'B' cannot be > 4": 1,
             "System.Data.SqlClient.SqlException (0x80131904): Error 2601, Level 14, State 1, Procedure ...": 1,
             'System.NotImplementedException: The method or operation is not implemented.\n': 2})
```
