using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NFluent;
using NUnit.Framework;
using ZeroLog.Config;
using ZeroLog.ConfigResolvers;
using ZeroLog.Tests.Support;

namespace ZeroLog.Tests
{
    [TestFixture]
    internal class HierarchicalAppenderResolverTests
    {
        // TODO names?

        private HierarchicalResolver _resolver;
        private ZeroLogJsonConfiguration _config;

        public class TestAppenderParameters
        {
            public string Name { get; set; }

            public TestAppenderParameters(string name) => Name = name;
        }

        [SetUp]
        public void SetUp()
        {
            _resolver = new HierarchicalResolver();

            _config = new ZeroLogJsonConfiguration();
            _config.RootLogger = new LoggerDefinition(string.Empty, Level.Info, false, LogMessagePoolExhaustionStrategy.Default, "A");

            _config.Appenders = new[]
            {
                new AppenderDefinition {Name = "A", AppenderTypeName = typeof(TestAppender).FullName},
                new AppenderDefinition {Name = "B", AppenderTypeName = typeof(TestAppender).FullName},
                new AppenderDefinition {Name = "C", AppenderTypeName = typeof(TestAppender).FullName}
            };
        }

        [Test]
        public void should_resolve_root()
        {
            _resolver.Build(_config);

            var appenders = _resolver.ResolveLogConfig("test").Appenders;

            appenders.Single().ShouldNotBeNull();
            // Check.That(appenders.Single().Name == "A");
        }

        [Test]
        public void should_resolve_child_node()
        {
            _config.Loggers = new[]
            {
                new LoggerDefinition("Abc.Zebus", Level.Info, false, LogMessagePoolExhaustionStrategy.Default),
                new LoggerDefinition("Abc.Zebus.Dispatch", Level.Info, false, LogMessagePoolExhaustionStrategy.Default, "A")
            };

            _resolver.Build(_config);

            var appenders = _resolver.ResolveLogConfig("Abc.Zebus.Dispatch.Handler").Appenders;

            appenders.Single().ShouldNotBeNull();
            // Check.That(appenders.Single().Name == "A").IsTrue();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_resolve_child_and_parent_node(bool includeParents)
        {
            _config.Loggers = new[]
            {
                new LoggerDefinition("Abc.Zebus", Level.Info, false, LogMessagePoolExhaustionStrategy.Default, "B"),
                new LoggerDefinition("Abc.Zebus.Dispatch", Level.Error, includeParents, LogMessagePoolExhaustionStrategy.Default, "A"),
                new LoggerDefinition("Abc", Level.Info, false, LogMessagePoolExhaustionStrategy.Default, "C")
            };

            _resolver.Build(_config);

            var appenders = _resolver.ResolveLogConfig("Abc.Zebus.Dispatch.Handler").Appenders;

            appenders.Length.ShouldEqual(includeParents ? 2 : 1);
            // Check.That(appenders.Any(x => x.Name == "A")).IsTrue();
            // Check.That(appenders.Any(x => x.Name == "B")).Equals(includeParents);
            // Check.That(appenders.Any(x => x.Name == "C")).IsFalse();
        }

        [Test]
        public void should_not_have_the_same_appender_twice()
        {
            _config.Loggers = new[]
            {
                new LoggerDefinition("Abc.Zebus", Level.Info, true, LogMessagePoolExhaustionStrategy.Default, "A"),
                new LoggerDefinition("Abc", Level.Info, false, LogMessagePoolExhaustionStrategy.Default, "A", "B")
            };

            _resolver.Build(_config);

            var appenders = _resolver.ResolveLogConfig("Abc.Zebus.Dispatch.Handler").Appenders;

            Check.That(appenders.Length).Equals(2);
        }
    }
}
