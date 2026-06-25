using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Validation
{
    public sealed class PasswordPolicyAttribute : ValidationAttribute
    {
        public PasswordPolicyAttribute()
        {
            ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.";
        }

        public override bool IsValid(object? value)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
                return true;

            return password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }
    }
}
