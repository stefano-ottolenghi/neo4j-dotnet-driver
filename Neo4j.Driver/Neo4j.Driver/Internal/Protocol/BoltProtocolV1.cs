﻿// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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

using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV1 : IBoltProtocol
    {


        private readonly ITcpSocketClient _tcpSocketClient;
        private readonly BufferSettings _bufferSettings;
        private readonly ILogger _logger;

        public IBoltReader Reader { get; private set; }
        public IBoltWriter Writer { get; private set; }

        public BoltProtocolV1(ITcpSocketClient tcpSocketClient, BufferSettings bufferSettings, ILogger logger=null)
        {
            _tcpSocketClient = tcpSocketClient;
            _bufferSettings = bufferSettings;
            _logger = logger;
            CreateReaderAndWriter(BoltProtocolPackStream.V1);
        }

        public bool ReconfigIfNecessary(string serverVersion)
        {
            var version = ServerVersion.Version(serverVersion);
            if ( version >= ServerVersion.V3_2_0 )
            {
                return false;
            }

            // downgrade PackStream to not support byte array.
            CreateReaderAndWriter(BoltProtocolPackStream.V1NoByteArray);
            return true;
        }

        private void CreateReaderAndWriter(IPackStreamFactory packStreamFactory)
        {
            Reader = new BoltReader(_tcpSocketClient.ReadStream, _bufferSettings.DefaultReadBufferSize,
                _bufferSettings.MaxReadBufferSize, _logger, packStreamFactory);
            Writer = new BoltWriter(_tcpSocketClient.WriteStream, _bufferSettings.DefaultWriteBufferSize,
                _bufferSettings.MaxWriteBufferSize, _logger, packStreamFactory);
        }
    }
}
