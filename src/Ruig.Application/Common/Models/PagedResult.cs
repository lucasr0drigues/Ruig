using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Models
{
    public sealed record PagedResult<T>(
        int Page,
        int PageSize,
        int TotalCount,
        IReadOnlyList<T> Items);
}
