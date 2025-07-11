﻿using UnitTests.Helpers;
using ViaEventAssociation.Core.Domain.Aggregates.Events;
using ViaEventAssociation.Core.Tools.OperationResult;

namespace UnitTests.Core.Domain.Aggregates.Events.UseCasesTests;

public class EventTestsId8
{
    [Fact]
    public void Id8_S1_SuccessfullyReadiesEvent_WhenAllFieldsValid()
    {
        var veaEvent = EventFactory.Init()
            .WithTitle("Summer Party")
            .WithDescription("Join us for a summer celebration!")
            .WithTimeRange(EventTimeRange.ValidNonDefault())
            .WithMaxGuests(25)
            .WithVisibility(EventVisibility.Public)
            .WithStatus(EventStatus.Draft)
            .Build();

        var result = veaEvent.Ready();

        Assert.True(result.IsSuccess);
        Assert.Equal(EventStatus.Ready, veaEvent.Status);
    }

    [Fact]
    public void Id8_F1_Failure_WhenFieldsAreInvalid()
    {
        // with "no set" values
        var eventWithInvalidValues = EventFactory.Init()
            .WithStatus(EventStatus.Draft)
            .WithTitle(null) // OR default
            .WithDescription(null) // OR default
            .WithTimeRange(null) // OR default
            // Visibility IS NOT set
            .WithMaxGuests(1) //
            .Build();

        var result = eventWithInvalidValues.Ready();
        Assert.False(result.IsSuccess);

        Assert.Contains(result.Errors, e => e == Error.EventTitleCannotBeDefaultOrEmpty);
        Assert.Contains(result.Errors, e => e == Error.EventDescriptionCannotBeDefault);
        Assert.Contains(result.Errors, e => e == Error.EventTimeRangeMissing);
        Assert.Contains(result.Errors, e => e == Error.EventVisibilityMustBeSet);
        Assert.Contains(result.Errors, e => e == Error.GuestsMaxNumberTooSmall);

        // with default values
        eventWithInvalidValues = EventFactory.Init()
            .WithStatus(EventStatus.Draft)
            .WithTitle(EventTitle.Default().Value)
            .WithDescription(EventDescription.Default().Value)
            .WithTimeRange(EventTimeRange.Default())
            // Visibility IS NOT set
            .WithMaxGuests(51) //
            .Build();

        result = eventWithInvalidValues.Ready();
        Assert.False(result.IsSuccess);

        Assert.Contains(result.Errors, e => e == Error.EventTitleCannotBeDefaultOrEmpty);
        Assert.Contains(result.Errors, e => e == Error.EventDescriptionCannotBeDefault);
        Assert.Contains(result.Errors, e => e == Error.EventTimeRangeCannotBeDefault);
        Assert.Contains(result.Errors, e => e == Error.EventVisibilityMustBeSet);
        Assert.Contains(result.Errors, e => e == Error.GuestsMaxNumberTooGreat);
    }


    [Fact]
    public void Id8_F2_Failure_WhenEventIsCancelled()
    {
        var veaEvent = EventFactory.Init()
            .WithStatus(EventStatus.Cancelled)
            .Build();

        var result = veaEvent.Ready();

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e == Error.EventAlreadyCancelled);
    }

    [Fact]
    public void Id8_F3_Failure_WhenStartDataTimeIsInThePast()
    {
        var startInPast = DateTime.Today.AddHours(8).AddDays(-2);
        var pastRange = EventTimeRange.Create(
            startInPast,
            startInPast.AddHours(3));
        Assert.True(pastRange.IsSuccess);

        var veaEvent = EventFactory.Init()
            .WithTimeRange(pastRange.Payload)
            .Build();

        var result = veaEvent.Ready();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e == Error.CannotReadyPastEvent);
    }

    [Fact]
    public void Id8_F4_Failure_WhenTitleIsDefault()
    {
        var veaEvent = EventFactory.Init()
            .WithTitle(EventTitle.Default().ToString())
            .Build();

        var result = veaEvent.Ready();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e == Error.EventTitleCannotBeDefaultOrEmpty);
    }
}