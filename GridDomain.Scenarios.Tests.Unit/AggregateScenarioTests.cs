﻿using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Configuration;
using GridDomain.EventSourcing;
using GridDomain.Scenarios;
using GridDomain.Scenarios.Builders;
using GridDomain.Scenarios.Runners;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Commands;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Scenario
{
    public class AggregateScenarioTests
    {
        private readonly Logger _log;

        public AggregateScenarioTests(ITestOutputHelper output)
        {
            _log = new XUnitAutoTestLoggerConfiguration(output).CreateLogger();
        }

        [Fact]
        public async Task When_defined_aggregate_handler_then_it_can_execute_commands_and_produce_events_with_builder()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            var run = await new AggregateScenarioBuilder<Balloon>()
                            .With(new BalloonDependencies())
                            .When(new InflateNewBallonCommand(42, aggregateId))
                            .Then(new BalloonCreated("42", aggregateId))
                            .Run
                            .Local(_log);

            var producedAggregate = run.Aggregate;

            //aggregate is changed 
            Assert.Equal("42", producedAggregate.Title);
            Assert.Equal(aggregateId, producedAggregate.Id);

            //event is produced and stored
            var producedEvent = run.ProducedEvents.OfType<BalloonCreated>()
                                   .First();
            Assert.Equal("42", producedEvent.Value);

            //scenario check is OK
            run.Check();
        }

        [Fact]
        public async Task When_defined_aggregate_handler_then_it_can_execute_commands_and_produce_events_with_explicit_runner()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            var run = await AggregateScenario.New<Balloon>()
                                             .With(new BalloonDependencies())
                                             .When(new InflateNewBallonCommand(42, aggregateId))
                                             .Then(new BalloonCreated("42", aggregateId))
                                             .Run
                                             .Local();

            var producedAggregate = run.Aggregate;

            //aggregate is changed 
            Assert.Equal("42", producedAggregate.Title);
            Assert.Equal(aggregateId, producedAggregate.Id);

            //event is produced and stored
            var producedEvent = run.ProducedEvents.OfType<BalloonCreated>()
                                   .First();
            Assert.Equal("42", producedEvent.Value);

            //scenario check is OK
            run.Check();
        }

        [Fact]
        public async Task When_defined_aggregate_handler_then_it_can_execute_commands_and_produce_events()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();


            var run = await AggregateScenario.New<Balloon>()
                                             .With(new BalloonDependencies())
                                             .When(new InflateNewBallonCommand(42, aggregateId))
                                             .Then(new BalloonCreated("42", aggregateId))
                                             .Run
                                             .Local(_log);

            //aggregate is changed 
            Assert.Equal("42", run.Aggregate.Title);
            Assert.Equal(aggregateId, run.Aggregate.Id);

            //event is produced and stored
            var producedEvent = run.ProducedEvents.OfType<BalloonCreated>()
                                   .First();
            Assert.Equal("42", producedEvent.Value);

            //scenario check is OK
            run.Check();
        }

        [Fact]
        public async Task Future_events_aggregate_can_be_tested()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            var run = await AggregateScenario.New<Balloon>()
                                             .With(new BalloonDependencies())
                                             .When(new InflateNewBallonCommand(42, aggregateId))
                                             .Then(new BalloonCreated("42", aggregateId))
                                             .Run
                                             .Local(_log);

            //aggregate is changed 
            Assert.Equal("42", run.Aggregate.Title);
            Assert.Equal(aggregateId, run.Aggregate.Id);

            //event is produced and stored
            var producedEvent = run.ProducedEvents.OfType<BalloonCreated>()
                                   .First();
            Assert.Equal("42", producedEvent.Value);

            //scenario check is OK
            run.Check();
        }

        [Fact]
        public async Task When_defined_scenario_has_given_it_is_applied_even_without_command()
        {
            var aggregateId = "personA";

            var scenario = await AggregateScenario.New<ProgrammerAggregate>()
                                                  .Given(new PersonCreated(aggregateId, aggregateId))
                                                  .Run
                                                  .Local(_log);
            //aggregate is changed 
            Assert.Equal(aggregateId, scenario.Aggregate.PersonId);
            Assert.Equal(aggregateId, scenario.Aggregate.Id);

            //no events was produced
            Assert.Empty(scenario.ProducedEvents);
        }

        [Fact]
        public async Task When_aggregate_has_custom_dependency_factory_scenario_can_use_it()
        {
            var aggregateId = "personA";
            var factory = AggregateDependencies.ForCommandAggregate<ProgrammerAggregate>();
            var scenario = await AggregateScenario.New<ProgrammerAggregate>()
                                                  .With(factory )
                                                  .Given(new PersonCreated(aggregateId, aggregateId))
                                                  .Run
                                                  .Local(_log);
            //aggregate is changed 
            Assert.Equal(aggregateId, scenario.Aggregate.PersonId);
            Assert.Equal(aggregateId, scenario.Aggregate.Id);

            //no events was produced
            Assert.Empty(scenario.ProducedEvents);
        }

        [Fact]
        public async Task Given_factory_When_defined_scenario_has_given_it_is_applied_even_without_command()
        {
            var aggregateId = "personA";

            var scenario = await AggregateScenario.New<ProgrammerAggregate>()
                                                  .With(new AggregateDependencies<ProgrammerAggregate>(null))
                                                  .Given(new PersonCreated(aggregateId, aggregateId))
                                                  .Run
                                                  .Local(_log);
            //aggregate is changed 
            Assert.Equal(aggregateId, scenario.Aggregate.PersonId);
            Assert.Equal(aggregateId, scenario.Aggregate.Id);

            //no events was produced
            Assert.Empty(scenario.ProducedEvents);
        }

        [Fact]
        public async Task When_defined_scenario_it_checks_for_produced_events_properties()
        {
            var aggregateId = "personA";

            await AggregateScenario.New<Balloon>()
                                   .With(new BalloonDependencies())
                                   .When(new InflateNewBallonCommand(42, aggregateId))
                                   .Then(new BalloonCreated("420", aggregateId))
                                   .Run
                                   .Local(_log)
                                   .Check()
                                   .ShouldThrow<ProducedEventsDifferException>();
        }

        [Fact]
        public async Task When_defined_scenario_it_checks_for_produced_events_count()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            await AggregateScenario.New<Balloon>()
                                   .With(new BalloonDependencies())
                                   .When(new InflateNewBallonCommand(42, aggregateId))
                                   .Then(new BalloonCreated("420", aggregateId),
                                         new BalloonTitleChanged("42", aggregateId))
                                   .Run
                                   .Local(_log)
                                   .Check()
                                   .ShouldThrow<ProducedEventsCountMismatchException>();
        }

        [Fact]
        public async Task When_defined_scenario_try_execute_missing_command_on_default_handler_it_throws_exception()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            await AggregateScenario.New<Balloon>()
                                   .With(new BalloonDependencies())
                                   .When(new CreateBalanceCommand(42, aggregateId))
                                   .Then(new BalloonCreated("420", aggregateId),
                                         new BalloonTitleChanged("42", aggregateId))
                                   .Run
                                   .Local(_log)
                                   .Check()
                                   .ShouldThrow((Predicate<CannotFindAggregateCommandHandlerExeption>) null);
        }

        [Fact]
        public async Task When_defined_scenario_executes_command_with_exception_it_throws_command_exception()
        {
            var aggregateId = Guid.NewGuid()
                                  .ToString();

            await AggregateScenario.New<Balloon>()
                                   .With(new BalloonDependencies())
                                   .When(new PlanTitleWriteAndBlowCommand(43, aggregateId, TimeSpan.FromMilliseconds(50)))
                                   .Run
                                   .Local(_log)
                                   .Check()
                                   .ShouldThrow((Predicate<BalloonException>) null);
        }
    }
}