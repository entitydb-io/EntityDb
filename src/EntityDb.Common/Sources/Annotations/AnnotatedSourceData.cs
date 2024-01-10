﻿using EntityDb.Abstractions.Sources.Annotations;
using EntityDb.Abstractions.ValueObjects;

namespace EntityDb.Common.Sources.Annotations;

internal record AnnotatedSourceData<TData>
(
    Id SourceId,
    TimeStamp SourceTimeStamp,
    Id MessageId,
    TData Data,
    Pointer EntityPointer
) : IAnnotatedSourceData<TData>
{
    public static IAnnotatedSourceData<TData> CreateFromBoxedData
    (
        Id sourceId,
        TimeStamp sourceTimeStamp,
        Id messageId,
        object boxedData,
        Pointer entityPointer
    )
    {
        var dataAnnotationType = typeof(AnnotatedSourceData<>).MakeGenericType(boxedData.GetType());

        return (IAnnotatedSourceData<TData>)Activator.CreateInstance
        (
            dataAnnotationType,
            sourceId,
            sourceTimeStamp,
            messageId,
            boxedData,
            entityPointer
        )!;
    }
}
