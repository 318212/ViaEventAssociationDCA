﻿using System.Data;
using ViaEventAssociation.Core.Domain.Aggregates.Events.Entities;
using ViaEventAssociation.Core.Domain.Aggregates.Guests;
using ViaEventAssociation.Core.Domain.Common.Bases;
using ViaEventAssociation.Core.Tools.OperationResult;

namespace ViaEventAssociation.Core.Domain.Aggregates.Events;

public class VeaEvent : AggregateRoot<EventId>
{
    public EventTitle? Title { get; private set; }
    public EventDescription? Description { get; private set; }
    public EventTimeRange? TimeRange { get; private set; }
    public EventStatus? Status { get; private set; }
    public EventVisibility? Visibility { get; private set; }
    public MaxGuests MaxGuestsNo { get; private set; }

    private readonly List<Invitation> _invitations = new();
    public IReadOnlyList<Invitation> Invitations => _invitations.AsReadOnly();

    private readonly List<GuestId> _guestList = new(); // temporary storage for the guests
    public IReadOnlyList<GuestId> GuestList => _guestList.AsReadOnly();

    public int LocationMaxCapacity { get; private set; }

    private VeaEvent(
        EventId id,
        EventTitle? title,
        EventDescription? description,
        EventTimeRange? timeRange,
        EventVisibility? visibility,
        MaxGuests maxGuestsNo,
        EventStatus? status,
        int? locationMaxCapacity,
        List<GuestId> guests,
        List<Invitation> invitations)
        : base(id)
    {
        Title = title;
        Description = description;
        TimeRange = timeRange;
        Visibility = visibility;
        MaxGuestsNo = maxGuestsNo;
        Status = status ?? EventStatus.Draft;
        LocationMaxCapacity = locationMaxCapacity ?? 500;
        _guestList.AddRange(guests);
        _invitations = invitations;
    }

    public static Result<VeaEvent> Create()
    {
        var newEvent = new VeaEvent(
            EventId.CreateUnique(),
            EventTitle.Default(),
            EventDescription.Default(),
            EventTimeRange.Default(),
            EventVisibility.Private,
            MaxGuests.Default(),
            EventStatus.Draft,
            500,
            [],
            []);

        return Result<VeaEvent>.Success(newEvent);
    }

    public Result UpdateTitle(EventTitle newTitle)
    {
        if (Status != null && (Status.Equals(EventStatus.Active) || Status.Equals(EventStatus.Cancelled)))
            return Result.Failure(Error.ActiveOrCanceledEventCannotBeModified);
        Title = newTitle;
        Status = EventStatus.Draft;
        return Result.Success();
    }

    public Result UpdateDescription(EventDescription newDescription)
    {
        if (Status != null && (Status.Equals(EventStatus.Active) || Status.Equals(EventStatus.Cancelled)))
            return Result.Failure(Error.ActiveOrCanceledEventCannotBeModified);
        Description = newDescription;
        Status = EventStatus.Draft;
        return Result.Success();
    }

    public Result UpdateTimeRange(EventTimeRange newTimeRange)
    {
        var errors = new List<Error>();

        if (Status != null && (Status.Equals(EventStatus.Active) || Status.Equals(EventStatus.Cancelled)))
            errors.Add(Error.ActiveOrCanceledEventCannotBeModified);

        var result = EventTimeRange.Validate(newTimeRange);

        if (result.IsSuccess) TimeRange = newTimeRange;
        if (result.IsFailure) errors.AddRange(result.Errors);

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors.ToArray());
    }

    public Result InviteGuest(GuestId guestId)
    {
        var invitation = Invitation.Create(guestId);
        if (_invitations.Any(i => i.GuestId == invitation.GuestId))
            return Result.Failure(Error.GuestAlreadyInvited);

        if (_guestList.Contains(invitation.GuestId))
            return Result.Failure(Error.GuestAlreadyJoined);

        if (IsEventFull())
            return Result.Failure(Error.NoMoreRoom);

        if (Equals(Status, EventStatus.Draft) || Equals(Status, EventStatus.Cancelled))
            return Result.Failure(Error.CanOnlyInviteToReadyOrActiveEvent);

        _invitations.Add(invitation);
        return Result.Success();
    }

    public Result Participate(GuestId guestId)
    {
        if (_guestList.Contains(guestId))
            return Result.Failure(Error.GuestAlreadyJoined);

        if (!Equals(Visibility, EventVisibility.Public))
            return Result.Failure(Error.EventIsNotPublic);

        if (!Equals(Status, EventStatus.Active))
            return Result.Failure(Error.OnlyActiveEventsCanBeJoined);

        if (IsEventFull())
            return Result.Failure(Error.NoMoreRoom);

        if (TimeRange!.StartTime < DateTime.Now) // TimeRange cannot be null here because Active events have a valid TimeRange
            return Result.Failure(Error.TooLate);

        _guestList.Add(guestId);
        return Result.Success();
    }

    public Result Activate()
    {
        if (Equals(EventStatus.Active, Status)) return Result.Success();

        if (!Equals(EventStatus.Ready, Status))
        {
            var readyResult = Ready();
            if (readyResult.IsFailure)
                return Result.Failure([Error.ActivateFailure, ..readyResult.Errors]);
        }

        Status = EventStatus.Active;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != null && Status.Equals(EventStatus.Cancelled))
            return Result.Failure(Error.EventAlreadyCancelled);

        Status = EventStatus.Cancelled;
        return Result.Success();
    }

    public override string ToString() =>
        $"{Title?.ToString()} ({Status?.ToString()})";

    public Result UpdateMaxGuests(MaxGuests newMaxGuests)
    {
        if (Equals(Status, EventStatus.Active) && newMaxGuests.Value < MaxGuestsNo.Value)
            return Result.Failure([Error.DecreaseMaxGuestsImpossible, Error.EventAlreadyActive]);

        if (Equals(Status, EventStatus.Cancelled))
            return Result.Failure([Error.UpdateMaxGuestsImpossible, Error.EventAlreadyCancelled]);

        if (newMaxGuests.Value > LocationMaxCapacity)
            return Result.Failure([Error.UpdateMaxGuestsImpossible, Error.MaxGuestAboveLocationCapacity]);

        var result = MaxGuests.Validate(newMaxGuests.Value);
        if (result.IsSuccess)
        {
            MaxGuestsNo = newMaxGuests;
            if (Equals(Status, EventStatus.Ready))
                Status = EventStatus.Draft;
        }

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors.ToArray());
    }

    public Result UpdateVisibility(EventVisibility newVisibility)
    {
        if (Equals(Status, EventStatus.Cancelled))
            return Result.Failure([Error.UpdateVisibilityImpossible, Error.EventAlreadyCancelled]);

        if (Equals(Visibility, EventVisibility.Public) && newVisibility.Equals(EventVisibility.Private))
            Status = EventStatus.Draft;

        Visibility = newVisibility;


        return Result.Success();
    }

    public Result Ready()
    {
        var errors = new List<Error>();

        if (Equals(Status, EventStatus.Active)) return Result.Failure(Error.EventAlreadyActive);
        if (Equals(Status, EventStatus.Cancelled)) return Result.Failure(Error.EventAlreadyCancelled);

        if (Title is null || Equals(EventTitle.Default(), Title)) errors.Add(Error.EventTitleCannotBeDefaultOrEmpty);
        if (Description is null || Equals(EventDescription.Default(), Description)) errors.Add(Error.EventDescriptionCannotBeDefault);
        if (Equals(EventTimeRange.Default(), TimeRange)) errors.Add(Error.EventTimeRangeCannotBeDefault);
        if (TimeRange == null) errors.Add(Error.EventTimeRangeMissing);
        if (TimeRange?.StartTime < DateTime.Now) errors.Add(Error.CannotReadyPastEvent);
        if (Equals(Visibility, null)) errors.Add(Error.EventVisibilityMustBeSet);
        if (MaxGuests.Validate(MaxGuestsNo.Value) is { IsFailure: true } result) errors.AddRange(result.Errors);

        if (errors.Count > 0)
            return Result.Failure(errors.ToArray());

        // if no errors:
        Status = EventStatus.Ready;
        return Result.Success();
    }

    public static Result<VeaEvent> Create(EventTitle? title, EventDescription? description, EventTimeRange timeRange, EventVisibility? visibility,
        MaxGuests maxGuests, EventStatus? status, int locationMaxCapacity, List<GuestId> guests, List<Invitation> invitations)
    {
        var newEvent = new VeaEvent(
            EventId.CreateUnique(),
            title,
            description,
            timeRange,
            visibility,
            maxGuests,
            status,
            locationMaxCapacity,
            guests,
            invitations);

        return Result<VeaEvent>.Success(newEvent);
    }

    public Result CancelParticipation(GuestId guestId)
    {
        if (TimeRange != null && TimeRange.StartTime < DateTime.Now)
            return Result.Failure(Error.ActiveOrCanceledEventCannotBeModified);

        if (GuestList.Contains(guestId))
            _guestList.Remove(guestId);

        return Result.Success();
    }

    private bool IsEventFull()
    {
        var acceptedInvitationNo = Invitations.Count(i => i.Status.Equals(InvitationStatus.Approved));
        return _guestList.Count + acceptedInvitationNo >= MaxGuestsNo.Value;
    }

    public Result AcceptInvitation(GuestId guestId)
    {
        var invi = _invitations.FirstOrDefault(i => i.GuestId == guestId);
        if (invi is null)
            return Result.Failure(Error.InvitationNotFound);

        if (Equals(Status, EventStatus.Cancelled))
            return Result.Failure(Error.CancelledEventsCannotBeJoined);

        if (Equals(Status, EventStatus.Ready) || Equals(Status, EventStatus.Draft))
            return Result.Failure(Error.JoinUnstartedEventImpossible);

        if (TimeRange.StartTime < DateTime.Now)
            return Result.Failure(Error.TooLate);

        if (invi.Status.Equals(InvitationStatus.Approved))
            return Result.Failure(Error.InvitationAlreadyApproved);

        if (invi.Status.Equals(InvitationStatus.Rejected))
            return Result.Failure(Error.InvitationAlreadyRejected);

        if (IsEventFull())
            return Result.Failure(Error.NoMoreRoom);

        return invi.Approve();
    }

    public bool HasAcceptedInvitation(GuestId guestId)
    {
        var invitation = Invitations.FirstOrDefault(i => i.GuestId == guestId);

        if (invitation is null)
            return false;

        return invitation.Status.Equals(InvitationStatus.Approved);
    }

    public Result DeclineInvitation(GuestId guestId)
    {
        var invitation = Invitations.FirstOrDefault(i => i.GuestId == guestId);

        if (invitation is null)
            return Result.Failure(Error.InvitationNotFound);

        if (Equals(Status, EventStatus.Cancelled))
            return Result.Failure(Error.DeclineImpossibleOnCancelledEvent);

        var result = invitation.Reject();
        return result;
    }
}