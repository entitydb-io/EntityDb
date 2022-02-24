﻿using Microsoft.Extensions.Logging;
using System;

namespace EntityDb.Common.Loggers;

internal record DefaultLogger(ILogger Logger) : Abstractions.Loggers.ILogger
{
    public void LogError(Exception exception, string extraMessage)
    {
        var eventId = new EventId(exception.GetHashCode(), exception.GetType().Name);

        Logger.LogError(eventId, exception, "{ExtraMessage}", extraMessage);
    }
}
