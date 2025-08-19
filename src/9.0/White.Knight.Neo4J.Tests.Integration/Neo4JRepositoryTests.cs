using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using White.Knight.Abstractions.Fluent;
using White.Knight.Domain;
using White.Knight.Neo4J.Extensions;
using White.Knight.Neo4J.Injection;
using White.Knight.Neo4J.Relationships;
using White.Knight.Neo4J.Tests.Integration.Repositories;
using White.Knight.Tests.Abstractions;
using White.Knight.Tests.Abstractions.Extensions;
using White.Knight.Tests.Abstractions.Repository;
using White.Knight.Tests.Abstractions.Tests;
using White.Knight.Tests.Domain;
using White.Knight.Tests.Domain.Specifications;
using Xunit.Abstractions;

namespace White.Knight.Neo4J.Tests.Integration
{
    public class Neo4JRepositoryTests(ITestOutputHelper helper)
        : AbstractedRepositoryTests(new Neo4JRepositoryTestContext(helper)), IAsyncLifetime
    {
        private static readonly Assembly RepositoryAssembly =
            Assembly
                .GetAssembly(typeof(AddressRepository));

        private readonly TestContainerManager _testContainerManager = new();

        public async Task InitializeAsync()
        {
            var context = GetContext() as Neo4JRepositoryTestContext;

            await
                _testContainerManager
                    .StartAsync(context.GetHostedPort());
        }

        public async Task DisposeAsync()
        {
            await
                _testContainerManager
                    .StopAsync();
        }

        public override Task Test_Search_By_Sub_Item_Query_Id()
        {
            // N/A. Sub query item searches are not invalid, but the nodes would likely be decoupled in Neo4j
            return
                Task
                    .CompletedTask;
        }

        [Fact]
        public async Task Test_Search_With_Basic_Relationship()
        {
            var context = (Neo4JRepositoryTestContext)GetContext();

            await
                context
                    .ArrangeRepositoryDataAsync();

            await
                context
                    .ActSearchWithBasicRelationshipAsync();

            context
                .AssertOrdersWereRetrievedAndMapped();
        }

        [Fact]
        public async Task Test_Search_With_No_Matching_Relationship()
        {
            var context = (Neo4JRepositoryTestContext)GetContext();

            await
                context
                    .ArrangeRepositoryDataAsync();

            await
                context
                    .ActSearchWithNoMatchingRelationshipAsync();

            context
                .AssertNoCustomersWereRetrieved();
        }

        [Fact]
        public async Task Test_Search_With_Matching_Relationship_And_Empty_Setter()
        {
            var context = (Neo4JRepositoryTestContext)GetContext();

            await
                context
                    .ArrangeRepositoryDataAsync();

            await
                context
                    .ActSearchWithBasicRelationshipEmptySetterAsync();

            context
                .AssertOrdersWereRetrievedButNotMapped();
        }

        [Fact]
        public async Task Test_Search_With_Matching_Relationship_And_Null_Setter()
        {
            var context = (Neo4JRepositoryTestContext)GetContext();

            await
                context
                    .ArrangeRepositoryDataAsync();

            await
                context
                    .ActSearchWithBasicRelationshipNullSetterAsync();

            context
                .AssertOrdersWereRetrievedButNotMapped();
        }

        [Fact]
        public async Task Test_Search_With_Advanced_Relationship()
        {
            var context = (Neo4JRepositoryTestContext)GetContext();

            await
                context
                    .ArrangeRepositoryDataAsync();

            await
                context
                    .ActSearchWithAdvancedRelationshipAsync();

            context
                .AssertFullNestingsWereRetrieved();
        }

        private class Neo4JRepositoryTestContext : RepositoryTestContextBase, IRepositoryTestContext
        {
            private readonly int _hostedPort;

            public Neo4JRepositoryTestContext(ITestOutputHelper testOutputHelper)
            {
                _hostedPort =
                    new Random()
                        .Next(10000, 11000);

                // TODO: debug
                _hostedPort = 7687;
                // debug

                // specify csv harness
                LoadTestConfiguration<Neo4JTestHarness>();

                Configuration =
                    InterceptConfiguration(Configuration, _hostedPort);

                // service initialisation
                ServiceCollection
                    .AddNeo4JRepositories(Configuration)
                    .AddAttributedNeo4JRepositories(RepositoryAssembly);

                // redirect ILogger output to Xunit console
                ServiceCollection
                    .ArrangeXunitOutputLogging(testOutputHelper);

                ServiceCollection
                    .AddNeo4JRepositoryFeatures(Configuration);

                LoadServiceProvider();
            }

            public int GetHostedPort()
            {
                return _hostedPort;
            }

            public async Task ActSearchWithBasicRelationshipAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new CustomerSpecByCustomerNumber(200)
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order>(
                                            "CREATED_ORDER",
                                            (c, o) => c.Orders.Add(o)))
                            );
            }

            public async Task ActSearchWithBasicRelationshipEmptySetterAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new CustomerSpecByCustomerNumber(200)
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order>(
                                            "CREATED_ORDER",
                                            (c, o) => { }))
                            );
            }

            public async Task ActSearchWithBasicRelationshipNullSetterAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new CustomerSpecByCustomerNumber(200)
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order>(
                                            "CREATED_ORDER",
                                            null))
                            );
            }

            public async Task ActSearchWithNoMatchingRelationshipAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new CustomerSpecByCustomerNumber(200)
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order>(
                                            "NO_MATCH_EXPECTED",
                                            (c, o) => c.Orders.Add(o)))
                            );
            }

            public async Task ActSearchWithAdvancedRelationshipAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new SpecificationByAll<Customer>()
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order, Customer, Address>(
                                            "CREATED_ORDER",
                                            (c, o) => c.Orders.Add(o),
                                            new RelationshipNavigation<Order, Customer, Address>(
                                                "CREATED_BY",
                                                (o, c) => o.Customer = c,
                                                new RelationshipNavigation<Customer, Address>(
                                                    "LIVES_AT",
                                                    (c, a) => c.Addresses.Add(a))
                                            )))
                            );
            }

            public async Task ActSearchWithAdvancedRelationshipAndNullSettersAsync()
            {
                Results =
                    await
                        Sut
                            .QueryAsync
                            (
                                new SpecificationByAll<Customer>()
                                    .ToQueryCommand()
                                    .WithRelationshipStrategy(
                                        new RelationshipNavigation<Customer, Order, Customer, Address>(
                                            "CREATED_ORDER",
                                            null,
                                            new RelationshipNavigation<Order, Customer, Address>(
                                                "CREATED_BY",
                                                null,
                                                new RelationshipNavigation<Customer, Address>(
                                                    "LIVES_AT",
                                                    null
                                            ))))
                            );
            }

            public void AssertOrdersWereRetrievedAndMapped()
            {
                var records =
                    Results?
                        .Records?
                        .ToList();

                Assert.NotNull(records);
                Assert.NotEmpty(records);

                foreach (var customer in records)
                {
                    Assert.NotEmpty(customer.Orders);
                }
            }

            public void AssertNoCustomersWereRetrieved()
            {
                var records =
                    Results?
                        .Records?
                        .ToList();

                Assert.NotNull(records);
                Assert.Empty(records);
            }

            public void AssertOrdersWereRetrievedButNotMapped()
            {
                var records =
                    Results?
                        .Records?
                        .ToList();

                Assert.NotNull(records);
                Assert.NotEmpty(records);

                foreach (var customer in records)
                {
                    Assert.Empty(customer.Orders);
                }
            }

            public void AssertFullNestingsWereRetrieved()
            {
                var customers =
                    Results?
                        .Records?
                        .ToList();

                Assert.NotNull(customers);
                Assert.NotEmpty(customers);

                var expectedCount = 0;
                foreach (var customer in customers)
                {
                    Assert.NotEmpty(customer.Orders);
                    expectedCount++;
                    foreach (var customerOrder in customer.Orders)
                    {
                        var orderCustomer = customerOrder.Customer;
                        Assert.NotNull(orderCustomer);
                        expectedCount++;
                        foreach (var customerAddress in orderCustomer.Addresses)
                        {
                            Assert.NotNull(customerAddress);
                            expectedCount++;
                        }
                    }
                }

                Assert.Equal(532, expectedCount);
            }

            private static IConfigurationRoot InterceptConfiguration(IConfigurationRoot existingConfiguration, int hostedPort)
            {
                var inMemoryCollection = new Dictionary<string, string>
                {
                    ["Neo4JRepositoryConfigurationOptions:DbUri"] = $"neo4j://localhost:{hostedPort}"
                };

                // Add the in-memory collection to the configuration
                return new ConfigurationBuilder()
                    .AddConfiguration(existingConfiguration)
                    .AddInMemoryCollection(inMemoryCollection)
                    .Build();
            }
        }
    }
}