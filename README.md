# log4net
Tools for log4net users

## ForwardingAppenderAsync
Appender forwards LoggingEvents to a list of attached appenders asynchronously.

The example of xml configuration:
```
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <Fix value="260"/>
    <appender-ref ref="RollingFileAppender" />
</appender>
```
### Benchmark

#### 100 sequential info-logs:

|                        Method |     Mean |
|------------------------------ |---------:|
| RollingFileAppender           | 17.79 ms |
| Forwarded RollingFileAppender |  0.05 ms |

#### 1000 parallel info-logs:

|                        Method |      Mean |
|------------------------------ |----------:|
| RollingFileAppender           | 169.46 ms |
| Forwarded RollingFileAppender |   0.49 ms |

## Log Analyzer
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
