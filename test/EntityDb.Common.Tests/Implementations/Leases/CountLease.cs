﻿using EntityDb.Abstractions.Leases;

namespace EntityDb.Common.Tests.Implementations.Leases;

public record CountLease(ulong Number) : ILease
{
    public string Scope => "";
    public string Label => "";
    public string Value => $"{Number}";
}