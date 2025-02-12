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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.MessageHandling;

internal sealed class ResponsePipeline : IResponsePipeline
{
    private const string MessagePattern = "S: {0}";

    private readonly ConcurrentQueue<IResponseHandler> _handlers;
    private readonly ILogger _logger;

    private IResponsePipelineError _error;

    public ResponsePipeline(ILogger logger)
    {
        _handlers = new ConcurrentQueue<IResponseHandler>();
        _logger = logger;
        _error = null;
    }

    public bool IsHealthy(out Exception error)
    {
        if (_error != null)
        {
            error = _error.Exception;
            return false;
        }

        error = null;
        return true;
    }

    public bool HasNoPendingMessages => _handlers.IsEmpty;

    public void Enqueue(IResponseHandler handler)
    {
        _handlers.Enqueue(handler ?? throw new ArgumentNullException(nameof(handler)));
    }

    public void AssertNoFailure()
    {
        _error?.EnsureThrown();
    }

    public void AssertNoProtocolViolation()
    {
        _error?.EnsureThrownIf<ProtocolException>();
    }

    public void OnSuccess(IDictionary<string, object> metadata)
    {
        LogSuccess(metadata);
        var handler = Dequeue();
        handler.OnSuccess(metadata);
    }

    public void OnRecord(object[] fieldValues)
    {
        LogRecord(fieldValues);
        var handler = Peek();
        handler.OnRecord(fieldValues);
    }

    public void OnFailure(FailureMessage failureMessage)
    {
        LogFailure(failureMessage.Code, failureMessage.Message);
        var handler = Dequeue();
        _error = new ResponsePipelineError(ErrorExtensions.ParseServerException(failureMessage));
        handler.OnFailure(_error);
    }

    public void OnIgnored()
    {
        LogIgnored();
        var handler = Dequeue();

        if (_error != null)
        {
            handler.OnFailure(_error);
        }
        else
        {
            handler.OnIgnored();
        }
    }

    public IResponseHandler Dequeue()
    {
        if (_handlers.TryDequeue(out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException("No handlers registered");
    }

    internal IResponseHandler Peek()
    {
        if (_handlers.TryPeek(out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException("No handlers registered");
    }

    private void LogSuccess(IDictionary<string, object> meta)
    {
        if (_logger.IsDebugEnabled())
        {
            _logger.Debug(MessagePattern, new SuccessMessage(meta));
        }
    }

    private void LogRecord(object[] fields)
    {
        if (_logger.IsDebugEnabled())
        {
            _logger.Debug(MessagePattern, new RecordMessage(fields));
        }
    }

    private void LogFailure(string code, string message)
    {
        if (_logger.IsDebugEnabled())
        {
            _logger.Debug(MessagePattern, new FailureMessage(code, message));
        }
    }

    private void LogIgnored()
    {
        if (_logger.IsDebugEnabled())
        {
            _logger.Debug(MessagePattern, IgnoredMessage.Instance);
        }
    }
}
