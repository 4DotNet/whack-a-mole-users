using Wam.Core.ErrorCodes;

namespace Wam.Users.ErrorCodes;

public abstract class UserErrorCode : WamErrorCode
{
    public static UserErrorCode UserNotFound => new UserNotFound();

    public override string Namespace => $"{base.Namespace}.Users";
}

public class UserNotFound : UserErrorCode
{
    public override string Code => "UserNotFound";
}