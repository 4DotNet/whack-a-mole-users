using Wam.Core.Exceptions;
using Wam.Users.ErrorCodes;

namespace Wam.Users.Exceptions;

public class WamUserException(UserErrorCode error, string message) : WamException(error, message);