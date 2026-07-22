#nullable enable

using System;

namespace Screenbox.Core.Models;

public sealed record class SqlParameterDto
{
    public string Name { get; set; } = string.Empty;

    public object Value { get; set; } = DBNull.Value;
}
