using Dapr.Client;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
    TelemetryClient telemetry,
    DaprClient daprClient) : IUsersService
{

    private const string StateStoreName = "statestore";

    private readonly Dictionary<string, string> defaultCacheMetaData = new()
    {
        {
            "ttlInSeconds", "900"
        }
    };

    public async Task<UserDetailsDto> Create(UserCreateDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating user {@User}", dto);
        var user = User.Create(dto.DisplayName, dto.EmailAddress);
        if (await SaveAndUpdateCache(user, cancellationToken) == false)
        {
            telemetry.TrackEvent(new EventTelemetry
            {
                Name = "newUserCreated",
                Timestamp = DateTimeOffset.UtcNow
            });
            throw new Exception("Failed to save user");
        }

        return user.ToDto();
    }



    public Task<UserDetailsDto> Get(Guid id, CancellationToken cancellationToken)
    {
        return GetFromStateStoreOrRepository(id, cancellationToken);
    }

    private async Task<UserDetailsDto> GetFromStateStoreOrRepository(Guid userId, CancellationToken cancellationToken)
    {
        var stateStoreValue = await daprClient.GetStateAsync<UserDetailsDto>(StateStoreName,
            CacheName.UserDetails(userId),
            cancellationToken: cancellationToken);
        if (stateStoreValue != null)
        {
            return stateStoreValue;
        }

        var userDetailsFromRepository = await GetUserDetailsFromRepository(userId, cancellationToken);
        await daprClient.SaveStateAsync(StateStoreName,
            CacheName.UserDetails(userId), userDetailsFromRepository,
            metadata: defaultCacheMetaData,
            cancellationToken: cancellationToken);
        return userDetailsFromRepository;
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
            await daprClient.SaveStateAsync(
                StateStoreName,
                CacheName.UserDetails(domainModel.Id),
                domainModel.ToDto(),
                metadata: defaultCacheMetaData,
                cancellationToken: cancellationToken);
        }

        return result;
    }
}