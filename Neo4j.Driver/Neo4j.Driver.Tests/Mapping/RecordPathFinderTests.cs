﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class RecordPathFinderTests
{
    [Fact]
    public void TryGetValueByPath_PathMatchesFieldName_ReturnsTrue()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(new[] { "testField" }, new object[] { "testValue" });

        var result = recordPathFinder.TryGetValueByPath(record, "testField", out var value);

        result.Should().BeTrue();
        value.Should().Be("testValue");
    }

    [Fact]
    public void TryGetValueByPath_PathDoesNotMatchFieldName_ReturnsFalse()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(new[] { "testField" }, new object[] { "testValue" });

        var result = recordPathFinder.TryGetValueByPath(record, "nonExistentField", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotAndMatchesFieldNameAndPropertyName_ReturnsTrue()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "TESTfield.testProperty", out var value);

        result.Should().BeTrue();
        value.Should().Be("testValue");
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotButDoesNotMatchFieldName_ReturnsFalse()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "nonExistentField.testProperty", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotMatchesFieldNameButNotPropertyName_ReturnsFalse()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "testField.nonExistentProperty", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotAndMatchesFieldNameAndPropertyName_ReturnsTrue_CaseInsensitive()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "testField.TESTproperty", out var value);

        result.Should().BeTrue();
        value.Should().Be("testValue");
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotButDoesNotMatchFieldName_ReturnsFalse_CaseInsensitive()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "nonExistentField.testProperty", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotMatchesFieldNameButNotPropertyName_ReturnsFalse_CaseInsensitive()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new object[] { new Dictionary<string, object> { { "testProperty", "testValue" } } });

        var result = recordPathFinder.TryGetValueByPath(record, "testField.nonExistentProperty", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValueByPath_PathContainsDotMatchesFieldButNotEntity_ReturnsFalse_CaseInsensitive()
    {
        var recordPathFinder = new RecordPathFinder();
        var record = TestRecord.Create(
            new[] { "testField" },
            new[] { "testValue" });

        var result = recordPathFinder.TryGetValueByPath(record, "testField.nonExistentProperty", out var value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }
}
