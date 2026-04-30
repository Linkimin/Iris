using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Domain.Common
{
    internal class AggregateRoot
    {
        private readonly List<DomainEvent> _domainEvents = [];

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
