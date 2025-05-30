// using ViaEventAssociation.Core.Tools.OperationResult;
//
// namespace ViaEventAssociation.Core.Domain.Aggregates.Guests;
//
// public class Guests
// {
//     // TODO: make Value objects first according to the domain model and assign them to the properties below:
//     private Guid Id { get; } = Guid.NewGuid();
//     private string Name { get; set; }
//     private string Email { get; set; }
//     private Uri ProfilePictureUrl { get; set; }
//
//     private Guests(string email, string firstName, string lastName, Uri profilePicture)
//     {
//         // TODO: add proper validation based on the use cases / requirements
//         Email = email;
//         Name = $"{firstName} {lastName}";
//         ProfilePictureUrl = profilePicture;
//     }
//
//     public static Result<Guests> CreateGuest(string email, string firstName, string lastName, Uri profilePicture)
//     {
//         // TODO: check if all validation is implemented
//         if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
//         {
//             return Result<Guests>.Failure("Invalid input for creating a guest.");
//         }
//
//         Guests guest = new Guests(email, firstName, lastName, profilePicture);
//         return Result<Guests>.Success(guest);
//     }
//
//     public Result<Guests> ParticipateInEvent(Guid guestId, Guid eventId)
//     {
//         // TODO: add proper implementation of participation
//         if (guestId != Id)
//         {
//             return Result<Guests>.Failure("Guests ID mismatch.");
//         }
//         return Result<Guests>.Success(this);
//     }
//
//     public Result<Guests> CancelMyParticipation(Guid guestId, Guid eventId)
//     {
//         // TODO: add proper implementation of canceling the participation
//         if (guestId != Id)
//         {
//             return Result<Guests>.Failure("Guests ID mismatch.");
//         }
//         return Result<Guests>.Success(this);
//     }
//
//     public Result<Guests> AcceptInvitation(Guid guestId, Guid invitationId)
//     {
//         // TODO: add proper implementation of accepting the invitation
//         if (guestId != Id)
//         {
//             return Result<Guests>.Failure("Guests ID mismatch.");
//         }
//         return Result<Guests>.Success(this);
//     }
//
//     public Result<Guests> DeclineInvitation(Guid guestId, Guid invitationId)
//     {
//         // TODO: add proper implementation of declining the invitation
//         if (guestId != Id)
//         {
//             return Result<Guests>.Failure("Guests ID mismatch.");
//         }
//         return Result<Guests>.Success(this);
//     }
// }