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

    <appender-ref ref="DebugAppender" />
    <appender-ref ref="RollingFileAppender" />
    <appender-ref ref="AdoNetAppender" />
</appender>
```
### [Benchmark](https://github.com/ABorovtsov/log4net/blob/main/log4net.tools.benchmarks/ForwardingAppenderTest.cs)

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
