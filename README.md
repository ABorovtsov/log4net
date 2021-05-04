# log4net tools

## [ForwardingAppenderAsync](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/ForwardingAppenderAsync.cs)
Appender forwards LoggingEvents to a list of attached appenders asynchronously. It uses an internal queue and a worker task which dequeues items in background. The modes of handling the buffer overflow situation described [here](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/BufferOverflowBehaviour.cs).

The example of the minimal xml configuration:
```
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <appender-ref ref="RollingFileAppender" />
</appender>
```

The example of the advanced xml configuration:
```
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

## [ForwardingAppenderAsyncWithMetrics](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/Metrics/ForwardingAppenderAsyncWithMetrics.cs)
Grabs metrics: LatencyUs, BufferSize, AllocatedBytes in addition to the [ForwardingAppenderAsync](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools/ForwardingAppenderAsync.cs) functionality. The default output is Trace Info.

The example of the minimal xml configuration:
```
<appender name="Forwarding2RollingFileAppenderWithMetrics" type="log4net.tools.ForwardingAppenderAsyncWithMetrics, log4net.tools">
    <appender-ref ref="RollingFileAppender" />
</appender>
```

The example of the advanced xml configuration:
```
<appender name="Forwarding2RollingFileAppenderWithMetrics" type="log4net.tools.ForwardingAppenderAsyncWithMetrics, log4net.tools">
    <MetricsWriter type="log4net.tools.integration.MetricsCsvWriter, log4net.tools.integration">
        <CsvFilePath value="data.csv"/>
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

The example of the output:
```csv
DateTime,LatencyUs,BufferSize,CallerName,AllocatedBytes
2021-05-04T11:49:22,2.00,76,DoAppend,5491168
2021-05-04T11:49:22,2.80,77,DoAppend,5587296
2021-05-04T11:49:22,318.80,77,Dequeue,5684840
2021-05-04T11:49:22,2.40,77,DoAppend,1269424
2021-05-04T11:49:22,2.50,78,DoAppend,1367776
2021-05-04T11:49:22,2.00,79,DoAppend,1466128
2021-05-04T11:49:22,2.30,80,DoAppend,1564480
2021-05-04T11:49:22,1.50,81,DoAppend,1662832
2021-05-04T11:49:22,273.80,81,Dequeue,1752992
```

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
