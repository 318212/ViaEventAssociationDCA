﻿using ViaEventAssociation.Core.Domain.Common.Bases;
using ViaEventAssociation.Core.Tools.OperationResult;

namespace ViaEventAssociation.Core.Domain.Aggregates.Events;

public class EventDescription : ValueObject
{
    public string Value { get; }

    private EventDescription(string value)
    {
        Value = value;
    }

    public static Result<EventDescription> Create(string? input)
    {
        return Validate(input);
    }

    private static Result<EventDescription> Validate(string? input)
    {
        if (input is null)
            input = "";

        if (input.Length > 250)
        {
            return Result<EventDescription>.Failure(Error.EventDescriptionCannotExceed250Characters);
        }

        return Result<EventDescription>.Success(new EventDescription(input));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static EventDescription Default()
    {
        return new EventDescription("");
    }
}