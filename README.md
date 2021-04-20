# log4net
Tools for log4net users

## ForwardingAppenderAsync
Appender forwards LoggingEvents to a list of attached appenders asynchronously.
```
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <appender-ref ref="RollingFileAppender" />
</appender>
```

## Log Analyzer
It's the python script which parses log4net logs and returns some stats.
```python
import pprint
from colorama import Fore, init
from simple_log_parser import SimpleLogParser

log_file_path = './log.txt'
error_stats = SimpleLogParser(log_file_path).errors_count(from_date='2021-04-15')

init(autoreset=True)
print(Fore.GREEN + 'Error Levels:')
pprint.pprint(error_stats[0])
print(Fore.GREEN + 'Error Messages:')
pprint.pprint(error_stats[1])
```