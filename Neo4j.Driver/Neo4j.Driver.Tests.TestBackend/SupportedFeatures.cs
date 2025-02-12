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

namespace Neo4j.Driver.Tests.TestBackend;

internal static class SupportedFeatures
{
    static SupportedFeatures()
    {
        FeaturesList = new List<string>
        {
            "Backend:MockTime",
            "ConfHint:connection.recv_timeout_seconds",
            "AuthorizationExpiredTreatment",
            "Detail:ClosedDriverIsEncrypted",
            "Detail:DefaultSecurityConfigValueEquality",
            "Feature:API:BookmarkManager",
            "Feature:API:ConnectionAcquisitionTimeout",
            "Feature:API:Driver.ExecuteQuery",
            "Feature:API:Driver.ExecuteQuery:WithAuth",
            "Feature:API:Driver:GetServerInfo",
            "Feature:API:Driver.IsEncrypted",
            "Feature:API:Driver:NotificationsConfig",
            "Feature:API:Driver.VerifyAuthentication",
            "Feature:API:Driver.VerifyConnectivity",
            "Feature:API:Driver.SupportsSessionAuth",
            "Feature:API:Liveness.Check",
            "Feature:API:Result.List",
            "Feature:API:Result.Peek",
            "Feature:API:Result.Single",
            "Feature:API:Session:NotificationsConfig",
            "Feature:API:RetryableExceptions",
            "Feature:API:Session:AuthConfig",
            "Feature:API:SSLClientCertificate",
            "Feature:API:SSLConfig",
            "Feature:API:SSLSchemes",
            "Feature:API:Summary:GqlStatusObjects",
            "Feature:API:Type.Temporal",
            "Feature:Auth:Bearer",
            "Feature:Auth:Custom",
            "Feature:Auth:Kerberos",
            "Feature:Auth:Managed",
            "Feature:Bolt:3.0",
            "Feature:Bolt:4.1",
            "Feature:Bolt:4.2",
            "Feature:Bolt:4.3",
            "Feature:Bolt:4.4",
            "Feature:Bolt:5.0",
            "Feature:Bolt:5.1",
            "Feature:Bolt:5.2",
            "Feature:Bolt:5.3",
            "Feature:Bolt:5.4",
            "Feature:Bolt:5.5",
            "Feature:Bolt:5.6",
            "Feature:Bolt:5.7",
            "Feature:Bolt:Patch:UTC",
            "Feature:Impersonation",
            //"Feature:TLS:1.1",
            "Feature:TLS:1.2",
            //"Feature:TLS:1.3",
            //"Optimization:ConnectionReuse",
            "Optimization:EagerTransactionBegin",
            "Optimization:ExecuteQueryPipelining",
            //"Optimization:ImplicitDefaultArguments",
            //"Optimization:MinimalResets",
            "Optimization:AuthPipelining",
            "Optimization:PullPipelining",
            //"Optimization:ResultListFetchAll",
        };
    }

    public static IReadOnlyList<string> FeaturesList { get; }
}
