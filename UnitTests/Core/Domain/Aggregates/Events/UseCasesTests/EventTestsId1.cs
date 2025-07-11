﻿using ViaEventAssociation.Core.Domain.Aggregates.Events;

namespace UnitTests.Core.Domain.Aggregates.Events.UseCasesTests;

public class EventTestsId1
{
    [Fact]
    public void Id1_S1_To_S4_CreateEvent_ShouldInitializeWithDefaults()
    {
        var result = VeaEvent.Create();

        Assert.True(result.IsSuccess);
        var ev = result.Payload!;
        Assert.Equal("Working title.", ev.Title.Value);
        Assert.Equal("", ev.Description.Value);
        Assert.Equal(EventStatus.Draft, ev.Status);
        Assert.Equal(EventVisibility.Private, ev.Visibility);
        Assert.Equal(5, ev.MaxGuestsNo.Value);
    }
}