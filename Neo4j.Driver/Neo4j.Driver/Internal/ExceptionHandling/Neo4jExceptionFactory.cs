﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Reflection;
using Neo4j.Driver.Internal.Helpers;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.ExceptionHandling;

internal class Neo4jExceptionFactory
{
    private record FactoryInfo(string Code, Func<FailureMessage, Exception, Neo4jException> ExceptionFactory);

    private readonly List<FactoryInfo> _exceptionFactories = new();
    private readonly SimpleWildcardHelper _simpleWildcardHelper = new();

    public Neo4jExceptionFactory()
    {
        // get all the types
        var codesAndExceptions = GetCodesAndExceptions();
        codesAndExceptions.Sort(CompareByCode);
        BuildExceptionFactories(codesAndExceptions);
    }

    private static List<(string code, Type exceptionType)> GetCodesAndExceptions()
    {
        return typeof(Neo4jException).Assembly
            .GetExportedTypes()
            .Where(t => typeof(Neo4jException).IsAssignableFrom(t))
            .Select(
                exceptionType => new
                {
                    exceptionType,
                    attr = exceptionType.GetCustomAttribute<ErrorCodeAttribute>()
                })
            .Where(t => t.attr is not null)
            .Select(t => (t.attr.Code, t.exceptionType))
            .ToList();
    }

    private int CompareByCode((string code, Type exceptionType) x, (string code, Type exceptionType) y)
    {
        // x comes before y if y matches x - this would happen if:
        // x = Error.Specific
        // y = Error.*
        // this means that less-specific wildcards are at the end of the list, so the first
        // matching wildcard will always be the most specific

        if (_simpleWildcardHelper.StringMatches(x.code, y.code))
        {
            return -1;
        }

        if (_simpleWildcardHelper.StringMatches(y.code, x.code))
        {
            return 1;
        }

        // otherwise, just compare the codes
        return string.Compare(x.code, y.code, StringComparison.InvariantCultureIgnoreCase);
    }

    private void BuildExceptionFactories(IEnumerable<(string code, Type exceptionType)> codesAndExceptions)
    {
        foreach (var (code, type) in codesAndExceptions)
        {
            var ctor = type.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(FailureMessage), typeof(Exception)],
                null);

            if (ctor is not null)
            {
                // create a factory function that will create the exception
                Neo4jException Factory(FailureMessage message, Exception inner)
                {
                    return (Neo4jException)ctor.Invoke([message, inner]);
                }

                _exceptionFactories.Add(new FactoryInfo(code, Factory));
            }
            else
            {
                throw new Neo4jException(
                    $"Neo4jException type {type.FullName} does not have a constructor that takes " +
                    $"a {nameof(FailureMessage)} and an {nameof(Exception)}");
            }

        }
    }

    public Neo4jException GetException(FailureMessage failureMessage)
    {
        var factoryInfo =
            _exceptionFactories.FirstOrDefault(f => _simpleWildcardHelper.StringMatches(failureMessage.Code, f.Code));

        if (factoryInfo is null)
        {
            return Neo4jException.Create(failureMessage);
        }

        var innerException = failureMessage.GqlCause != null ? GetException(failureMessage.GqlCause) : null;

        var exception = factoryInfo.ExceptionFactory(failureMessage, innerException);
        return exception;
    }
}
