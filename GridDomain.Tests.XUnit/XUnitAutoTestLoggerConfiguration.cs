using System;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Tests.Framework;
using NMoneys;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit
{
    public class XUnitAutoTestLoggerConfiguration : LoggerConfiguration
    {
        public XUnitAutoTestLoggerConfiguration(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Verbose)
        {
            WriteTo.XunitTestOutput(output);
            MinimumLevel.Is(level);
            Destructure.ByTransforming<Money>(r => new {r.Amount,r.CurrencyCode });
            Destructure.ByTransforming<Exception>(r => new {Type = r.GetType(), r.StackTrace });
            Destructure.ByTransforming<IMessageMetadata>(r => new {r.CasuationId, r.CorrelationId });
            Destructure.ByTransforming<ICommand>(r => new {r.Id});
        }
    }
}