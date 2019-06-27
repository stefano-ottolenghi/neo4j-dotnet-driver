﻿// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltStubServerTests
    {
        public Config Config { get; set; }

        public BoltStubServerTests(ITestOutputHelper output)
        {
            Config = new Config {EncryptionLevel = EncryptionLevel.None, DriverLogger = new TestDriverLogger(output)};
        }

        [RequireBoltStubServerFact]
        public async Task SendRoutingContextToServer()
        {
            using (BoltStubServer.Start("get_routing_table_with_context", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001/?policy=my_policy&region=china");
                using (var driver = GraphDatabase.Driver(uri, Config))
                {
                    var session = driver.Session();
                    try
                    {
                        var cursor = await session.RunAsync("MATCH (n) RETURN n.name AS name");
                        var records = await cursor.ToListAsync();

                        records.Count.Should().Be(2);
                        records[0]["name"].ValueAs<string>().Should().Be("Alice");
                        records[1]["name"].ValueAs<string>().Should().Be("Bob");
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task ShouldLogServerAddress()
        {
            var logs = new List<string>();
            var config = new Config
            {
                EncryptionLevel = EncryptionLevel.None,
                DriverLogger = new TestDriverLogger(logs.Add, ExtendedLogLevel.Debug)
            };
            using (BoltStubServer.Start("accessmode_reader_implicit", 9001))
            {
                using (var driver = GraphDatabase.Driver("bolt://localhost:9001", AuthTokens.None, config))
                {
                    var session = driver.Session(AccessMode.Read);
                    try
                    {
                        var cursor = await session.RunAsync("RETURN $x", new {x = 1});
                        var list = await cursor.ToListAsync(r => Convert.ToInt32(r[0]));

                        list.Should().HaveCount(1).And.Contain(1);
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }

            foreach (var log in logs)
            {
                if (log.StartsWith("[Debug]:[conn-"))
                {
                    log.Should().Contain("localhost:9001");
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task InvokeProcedureGetRoutingTableWhenServerVersionPermits()
        {
            using (BoltStubServer.Start("get_routing_table", 9001))
            {
                var uri = new Uri("bolt+routing://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                {
                    var session = driver.Session();
                    try
                    {
                        var cursor = await session.RunAsync("MATCH (n) RETURN n.name AS name");
                        var records = await cursor.ToListAsync();

                        records.Count.Should().Be(3);
                        records[0]["name"].ValueAs<string>().Should().Be("Alice");
                        records[1]["name"].ValueAs<string>().Should().Be("Bob");
                        records[2]["name"].ValueAs<string>().Should().Be("Eve");
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task CanSendMultipleBookmarks()
        {
            var bookmarks = new[]
            {
                "neo4j:bookmark:v1:tx5", "neo4j:bookmark:v1:tx29",
                "neo4j:bookmark:v1:tx94", "neo4j:bookmark:v1:tx56",
                "neo4j:bookmark:v1:tx16", "neo4j:bookmark:v1:tx68"
            };
            using (BoltStubServer.Start("multiple_bookmarks", 9001))
            {
                var uri = new Uri("bolt://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                {
                    var session = driver.Session(bookmarks);
                    try
                    {
                        var txc = await session.BeginTransactionAsync();
                        try
                        {
                            await txc.RunAsync("CREATE (n {name:'Bob'})");
                            await txc.CommitAsync();
                        }
                        catch
                        {
                            await txc.RollbackAsync();
                            throw;
                        }
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }

                    session.LastBookmark.Should().Be("neo4j:bookmark:v1:tx95");
                }
            }
        }

        [RequireBoltStubServerFact]
        public async Task ShouldOnlyResetAfterError()
        {
            using (BoltStubServer.Start("rollback_error", 9001))
            {
                var uri = new Uri("bolt://127.0.0.1:9001");
                using (var driver = GraphDatabase.Driver(uri, Config))
                {
                    var session = driver.Session();
                    try
                    {
                        var txc = await session.BeginTransactionAsync();
                        try
                        {
                            var result = await txc.RunAsync("CREATE (n {name:'Alice'}) RETURN n.name AS name");
                            var exception = await Record.ExceptionAsync(() => result.ConsumeAsync());

                            exception.Should().BeOfType<TransientException>();
                        }
                        finally
                        {
                            await txc.RollbackAsync();
                        }
                    }
                    finally
                    {
                        await session.CloseAsync();
                    }
                }
            }
        }
    }
}