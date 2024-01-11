using System;
using System.Diagnostics.CodeAnalysis;

namespace SimpleModpackDownloader.Infrastructure;

internal readonly record struct Result<T>(T? Value, Exception? Error)
{
    [MemberNotNullWhen(true, nameof(Value))]
    internal bool IsSucessful => this != default && Value is not null && Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(Exception error) => new(default, error);
}
