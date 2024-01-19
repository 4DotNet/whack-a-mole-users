using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Logging;
using Wam.Core.Cache;
using Wam.Users.DataTransferObjects;
using Wam.Users.DomainModels;
using Wam.Users.Enums;
using Wam.Users.Mappings;
using Wam.Users.Repositories;

namespace Wam.Users.Services;

public class UsersService(
    IUsersRepository usersRepository, 
    ILogger<UsersService> logger,
    ICacheClientFactory cacheClientFactory) : IUsersService
{
    public async Task<UserDetailsDto> Create(UserCreateDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating user {@User}", dto);
        var user = User.Create(dto.DisplayName, dto.EmailAddress);
        if (await SaveAndUpdateCache(user, cancellationToken) == false)
        {
            throw new Exception("Failed to save user");
        }
        return user.ToDto();
    }

    
    
    public  Task<UserDetailsDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var cacheClient = cacheClientFactory.CreateClient();
        var cacheKey = CacheName.UserDetails(id);
        return  cacheClient.GetOrInitializeAsync(() => GetUserDetailsFromRepository(id, cancellationToken), cacheKey);
    }

    private async Task<UserDetailsDto> GetUserDetailsFromRepository(Guid userId, CancellationToken cancellationToken)
    {
        var domainModel = await usersRepository.Get(userId, cancellationToken);
        return domainModel.ToDto();
    }

    public async Task<UserDetailsDto> Ban(Guid id, byte reasonId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Banning user {@Id} for reason {@ReasonId}", id, reasonId);
        var reason = ExclusionReason.FromId(reasonId);
        var domainModel = await usersRepository.Get(id, cancellationToken);
        domainModel.Exclude(reason);
        if (await SaveAndUpdateCache(domainModel, cancellationToken) == false)
        {
            throw new Exception("Failed to save user");
        }
        return domainModel.ToDto();
    }

    private async Task<bool> SaveAndUpdateCache(User domainModel, CancellationToken cancellationToken)
    {
        var result = await usersRepository.Save(domainModel, cancellationToken);
        if (result)
        {
            var cacheClient = cacheClientFactory.CreateClient();
            var cacheKey = CacheName.UserDetails(domainModel.Id);
            await cacheClient.SetAsAsync(cacheKey, domainModel.ToDto());
        }
        return result;
    }
}