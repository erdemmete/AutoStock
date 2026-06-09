namespace AutoStock.Repositories.Enums
{
    public enum UserSecurityTokenPurpose
    {
        PasswordSetup = 1,
        PasswordReset = 2,
        EmailVerification = 3,
        PhoneVerification = 4,
        LoginOtp = 5,
        SensitiveActionConfirmation = 6
    }
}