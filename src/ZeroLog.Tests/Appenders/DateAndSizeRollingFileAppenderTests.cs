﻿using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using ZeroLog.Appenders;
using ZeroLog.Configuration;
using ZeroLog.Formatting;
using ZeroLog.Tests.Support;

namespace ZeroLog.Tests.Appenders;

[TestFixture]
public class DateAndSizeRollingFileAppenderTests
{
    private DateAndSizeRollingFileAppender _appender;

    [SetUp]
    public void SetUp()
    {
        _appender = new DateAndSizeRollingFileAppender("TestLog")
        {
            PrefixPattern = "%date - %time - %thread - %level - %logger || ",
            FileNameRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"))
        };
    }

    [TearDown]
    public void Teardown()
    {
        _appender.Dispose();
        File.Delete(_appender.GetCurrentFileName());
    }

    [Test, RequiresThread]
    public void should_log_to_file()
    {
        var logMessage = new LogMessage("Test log message");
        logMessage.Initialize(new Log("TestLog"), LogLevel.Info);

        var formattedMessage = new FormattedLogMessage(logMessage.ToString().Length, ZeroLogConfiguration.Default);
        formattedMessage.SetMessage(logMessage);

        _appender.WriteMessage(formattedMessage);
        _appender.WriteMessage(formattedMessage);
        _appender.Flush();

        using var reader = new StreamReader(File.Open(_appender.CurrentFileName.ShouldNotBeNull(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite), _appender.Encoding);
        var text = reader.ReadToEnd();

        var expectedLine = $"{logMessage.Timestamp.Date:yyyy-MM-dd} - {logMessage.Timestamp.TimeOfDay:hh\\:mm\\:ss\\.fffffff} - {Thread.CurrentThread.ManagedThreadId} - INFO - TestLog || {logMessage}";
        text.ShouldEqual(expectedLine + Environment.NewLine + expectedLine + Environment.NewLine);
    }
}
