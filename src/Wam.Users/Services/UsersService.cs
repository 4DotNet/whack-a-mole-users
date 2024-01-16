using Microsoft.Extensions.Logging;
using Wam.Users.DataTransferObjects;
using Wam.Users.DomainModels;
using Wam.Users.Enums;
using Wam.Users.Mappings;
using Wam.Users.Repositories;

namespace Wam.Users.Services;

public class UsersService(IUsersRepository usersRepository, ILogger<UsersService> logger) : IUsersService
{
    public async Task<UserDetailsDto> Create(UserCreateDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating user {@User}", dto);
        var user = User.Create(dto.DisplayName, dto.EmailAddress);
        if (await usersRepository.Save(user, cancellationToken) == false)
        {
            throw new Exception("Failed to save user");
        }
        return user.ToDto();
    }

    public async Task<UserDetailsDto> Get(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting user {@Id}", id);
        var domainModel = await usersRepository.Get(id, cancellationToken);
        return domainModel.ToDto();
    }

    public async Task<UserDetailsDto> Ban(Guid id, byte reasonId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Banning user {@Id} for reason {@ReasonId}", id, reasonId);
        var reason = ExclusionReason.FromId(reasonId);
        var domainModel = await usersRepository.Get(id, cancellationToken);
        domainModel.Exclude(reason);
        if (await usersRepository.Save(domainModel, cancellationToken) == false)
        {
            throw new Exception("Failed to save user");
        }
        return domainModel.ToDto();
    }
}