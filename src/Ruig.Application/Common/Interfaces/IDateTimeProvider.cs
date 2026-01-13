using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
