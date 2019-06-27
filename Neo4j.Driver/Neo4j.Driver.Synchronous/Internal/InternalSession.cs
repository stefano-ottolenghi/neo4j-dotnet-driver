// Copyright (c) 2002-2019 "Neo4j,"
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

namespace Neo4j.Driver.Internal
{
    internal class InternalSession : ISyncSession
    {
        private readonly IReactiveSession _session;
        private readonly ISyncRetryLogic _retryLogic;
        private readonly SyncExecutor _syncExecutor;

        private bool _disposed;

        public InternalSession(IReactiveSession session, ISyncRetryLogic retryLogic, SyncExecutor syncExecutor)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _retryLogic = retryLogic ?? throw new ArgumentNullException(nameof(retryLogic));
            _syncExecutor = syncExecutor ?? throw new ArgumentNullException(nameof(syncExecutor));
        }

        public string LastBookmark => _session.LastBookmark;

        public IStatementResult Run(string statement)
        {
            return Run(new Statement(statement), TransactionConfig.Empty);
        }

        public IStatementResult Run(string statement, object parameters)
        {
            return Run(new Statement(statement, parameters.ToDictionary()), TransactionConfig.Empty);
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters)
        {
            return Run(new Statement(statement, parameters), TransactionConfig.Empty);
        }

        public IStatementResult Run(Statement statement)
        {
            return Run(statement, TransactionConfig.Empty);
        }

        #region BeginTransaction Methods

        public ISyncTransaction BeginTransaction()
        {
            return BeginTransaction(TransactionConfig.Empty);
        }

        public ISyncTransaction BeginTransaction(TransactionConfig txConfig)
        {
            return new InternalTransaction(
                _syncExecutor.RunSync(() => _session.BeginTransactionAsync(txConfig))
                    .CastOrThrow<IReactiveTransaction>(), _syncExecutor);
        }

        private ISyncTransaction BeginTransaction(AccessMode mode, TransactionConfig txConfig)
        {
            return new InternalTransaction(
                _syncExecutor.RunSync(() => _session.BeginTransactionAsync(mode, txConfig))
                    .CastOrThrow<IReactiveTransaction>(), _syncExecutor);
        }

        #endregion

        #region Transaction Methods

        public T ReadTransaction<T>(Func<ISyncTransaction, T> work)
        {
            return ReadTransaction(work, TransactionConfig.Empty);
        }

        public T ReadTransaction<T>(Func<ISyncTransaction, T> work, TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Read, work, txConfig);
        }

        public T WriteTransaction<T>(Func<ISyncTransaction, T> work)
        {
            return WriteTransaction(work, TransactionConfig.Empty);
        }

        public T WriteTransaction<T>(Func<ISyncTransaction, T> work, TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Write, work, txConfig);
        }

        internal T RunTransaction<T>(AccessMode mode, Func<ISyncTransaction, T> work, TransactionConfig txConfig)
        {
            return _retryLogic.Retry(() =>
            {
                using (var txc = BeginTransaction(mode, txConfig))
                {
                    try
                    {
                        var result = work(txc);
                        txc.Success();
                        return result;
                    }
                    catch
                    {
                        txc.Failure();
                        throw;
                    }
                }
            });
        }

        #endregion

        public IStatementResult Run(string statement, TransactionConfig txConfig)
        {
            return Run(new Statement(statement), txConfig);
        }

        public IStatementResult Run(string statement, IDictionary<string, object> parameters,
            TransactionConfig txConfig)
        {
            return Run(new Statement(statement, parameters), txConfig);
        }

        public IStatementResult Run(Statement statement, TransactionConfig txConfig)
        {
            return new InternalStatementResult(_syncExecutor.RunSync(() => _session.RunAsync(statement, txConfig)),
                _syncExecutor);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _syncExecutor.RunSync(() => _session.CloseAsync());
                }
            }

            _disposed = true;
        }
    }
}