﻿using System;
using System.Collections.Generic;
using System.Transactions;

namespace Rebus.Bus
{
    /// <summary>
    /// Implementation of <see cref="ITransactionContext"/> that is tied to an ambient .NET transaction.
    /// </summary>
    public class AmbientTransactionContext : IEnlistmentNotification, ITransactionContext
    {
        readonly Dictionary<string, object> items = new Dictionary<string, object>();

        /// <summary>
        /// Constructs the context, enlists it in the ambient transaction, and sets itself as the current context in <see cref="TransactionContext"/>.
        /// </summary>
        public AmbientTransactionContext()
        {
            if (Transaction.Current == null)
            {
                throw new InvalidOperationException("There's currently no ambient transaction associated with this thread." +
                                                    " You can only instantiate this class within a TransactionScope.");
            }
            Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            TransactionContext.Set(this);
        }

        /// <summary>
        /// Will be raised when it is time to commit the transaction. The transport should do its final
        /// commit work when this event is raised.
        /// </summary>
        public event Action DoCommit = delegate { };

        /// <summary>
        /// Will be raised before doing the actual commit
        /// </summary>
        public event Action BeforeCommit = delegate { };

        /// <summary>
        /// Will be raised in the event that the transaction should be rolled back.
        /// </summary>
        public event Action DoRollback = delegate { };

        /// <summary>
        /// Will be raised after a transaction has been rolled back
        /// </summary>
        public event Action AfterRollback = delegate { };

        /// <summary>
        /// Will be raised after all work is done, allowing you to clean up resources etc.
        /// </summary>
        public event Action Cleanup = delegate { };

        /// <summary>
        /// Returns true because we're always transactional when we're enlisted in an ambient transaction
        /// </summary>
        public bool IsTransactional { get { return true; } }

        /// <summary>
        /// Gives access to a dictionary of stuff that will be kept for the duration of the transaction.
        /// </summary>
        public object this[string key]
        {
            get { return items.ContainsKey(key) ? items[key] : null; }
            set { items[key] = value; }
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        /// <summary>
        /// Performs necessary commit actions, clearing the current <see cref="TransactionContext"/>
        /// </summary>
        public void Commit(Enlistment enlistment)
        {
            BeforeCommit();
            DoCommit();
            TransactionContext.Clear();
            enlistment.Done();
        }

        /// <summary>
        /// Performs necessary rollback actions, clearing the current <see cref="TransactionContext"/>
        /// </summary>
        public void Rollback(Enlistment enlistment)
        {
            DoRollback();
            enlistment.Done();
            TransactionContext.Clear();
            AfterRollback();
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}