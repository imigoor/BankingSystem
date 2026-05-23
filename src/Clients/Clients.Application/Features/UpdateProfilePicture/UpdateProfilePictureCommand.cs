using MediatR;

namespace Clients.Application.Features.UpdateProfilePicture;

public sealed record UpdateProfilePictureCommand(
    Guid ClientId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<string>;
