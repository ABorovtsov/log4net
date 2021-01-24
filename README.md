# log4net
Tools for log4net users

## ForwardingAppenderAsync
Appender forwards LoggingEvents to a list of attached appenders asynchronously.
```
<appender name="ForwardingAppenderAsync" type="log4net.tools.ForwardingAppenderAsync">
    <appender-ref ref="RollingFileAppender" />
</appender>
```