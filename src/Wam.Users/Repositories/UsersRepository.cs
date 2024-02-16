using Azure.Data.Tables;
using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Options;
using Wam.Core.Configuration;
using Wam.Core.ExtensionMethods;
using Wam.Core.Identity;
using Wam.Users.DomainModels;
using Wam.Users.Entities;
using Wam.Users.ErrorCodes;
using Wam.Users.Exceptions;

namespace Wam.Users.Repositories;

public class UsersRepository : IUsersRepository
{
    private const string TableName = "users";
    private const string PartitionKey = "users";
    private readonly TableClient _tableClient;

    public async Task<bool> Save(User user, CancellationToken cancellationToken)
    {
        var entity = new UserEntity
        {
            PartitionKey = PartitionKey,
            RowKey = user.Id.ToString(),
            DisplayName = user.DisplayName,
            EmailAddress = user.EmailAddress,
            ExclusionReason = user.ExclusionReason,
            Timestamp = DateTimeOffset.UtcNow
        };
        var response = await _tableClient.UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace,
            cancellationToken);
        return response.Status.IsHttpSuccessCode();
    }

    public async Task<User> Get(Guid userId, CancellationToken cancellationToken)
    {
        var entity =  await GetFromTableStorage(userId, cancellationToken);
        return new User(userId,
            entity.DisplayName,
            entity.EmailAddress,
            (byte?)entity.ExclusionReason);
    }

    private async Task<UserEntity> GetFromTableStorage(Guid userId, CancellationToken cancellationToken)
    {
        var cloudResponse = await _tableClient.GetEntityAsync<UserEntity>(PartitionKey, userId.ToString(),
            cancellationToken: cancellationToken);

        if (cloudResponse.HasValue)
        {
            return cloudResponse.Value;
        }

        throw new WamUserException(UserErrorCode.UserNotFound, $"The user with GUID {userId} could not be found");
    }

    public UsersRepository(
        IOptions<AzureServices> configuration)
    {
        var tableStorageUrl = $"https://{configuration.Value.UsersStorageAccountName}.table.core.windows.net";
        _tableClient = new TableClient(new Uri(tableStorageUrl), TableName,CloudIdentity.GetCloudIdentity());
    }
}