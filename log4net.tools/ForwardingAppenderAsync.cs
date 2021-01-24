using System;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.tools
{
    /// <summary>
    /// Appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsync : IAppender, IAppenderAttachable
    {
        public string Name { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.All;

        private AppenderAttachedImpl _appenderAttached;

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            loggingEvent.Fix = Fix;
            ThreadPool.QueueUserWorkItem(AsyncAppend, loggingEvent);
        }

        public void Close()
        {
            lock (this)
            {
                _appenderAttached?.RemoveAllAppenders();
            }
        }

        public void AddAppender(IAppender newAppender)
        {
            if (newAppender == null)
            {
                throw new ArgumentNullException(nameof(newAppender));
            }

            lock (this)
            {
                if (_appenderAttached == null)
                {
                    _appenderAttached = new AppenderAttachedImpl();
                }

                _appenderAttached.AddAppender(newAppender);
            }
        }

        public AppenderCollection Appenders
        {
            get
            {
                lock (this)
                {
                    return _appenderAttached == null 
                        ? AppenderCollection.EmptyCollection 
                        : _appenderAttached.Appenders;
                }
            }
        }

        public IAppender GetAppender(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            lock (this)
            {
                return _appenderAttached?.GetAppender(name);
            }
        }

        public void RemoveAllAppenders()
        {
            lock (this)
            {
                _appenderAttached?.RemoveAllAppenders();
                _appenderAttached = null;
            }
        }

        public IAppender RemoveAppender(IAppender appender)
        {
            if (appender == null)
            {
                return null;
            }

            lock (this)
            {
                return _appenderAttached?.RemoveAppender(appender);
            }
        }

        public IAppender RemoveAppender(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            lock (this)
            {
                return _appenderAttached?.RemoveAppender(name);
            }
        }

        private void AsyncAppend(object state)
        {
            if (state == null)
            {
                return;
            }

            if (state is LoggingEvent loggingEvent)
            {
                lock (this)
                {
                    _appenderAttached?.AppendLoopOnAppenders(loggingEvent);
                }
            }
        }
    }
}
