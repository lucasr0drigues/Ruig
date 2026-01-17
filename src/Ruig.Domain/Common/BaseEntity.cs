using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.Common
{
    public abstract class BaseEntity : IEquatable<BaseEntity>
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; init; }

        public virtual bool Equals(BaseEntity? other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
