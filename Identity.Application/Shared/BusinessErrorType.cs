namespace MyApp.Application.Shared;
    public enum BusinessErrorType
    {
        None = 0,
        EmailAlreadyExists,
        CanNotCreateUser,
        UserAlreadyExists,
        UserNotFound,
        InvalidToken,
        InvalidCredentials,
        InsufficientPermissions,
        ResourceNotFound,
        ValidationFailed,
        OperationNotAllowed,
        PaymentRequired,
        SubscriptionExpired,
        AuthenticatorSetupFailed,
        EmailNotConfirmed,
        AccountLocked,
        AccountDisabled,
        ConcurrencyConflict,
        Unknown,



        //----------------------------------------------
        InvalidImage,

}

