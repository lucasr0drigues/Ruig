using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Domain.Common
{
    public class DomainException : Exception
    {
        protected DomainException() { }

        public DomainException(string message) : base(message) { }

        public DomainException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
