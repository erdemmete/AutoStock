using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Interfaces;
using AutoStock.WEB.Models.Admin.Workshops;
using System.Net;

namespace AutoStock.WEB.Services;

public class AdminWorkshopInviteEmailService
{
    private readonly IEmailSender _emailSender;

    public AdminWorkshopInviteEmailService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task<ServiceResult<bool>> SendCreatedUserInviteAsync(
        AdminWorkshopUserCreatedViewModel createdUser,
        string setupUrl,
        string? workshopName = null)
    {
        if (string.IsNullOrWhiteSpace(createdUser.Email))
        {
            return ServiceResult<bool>.Success(true);
        }

        var brandName = string.IsNullOrWhiteSpace(workshopName)
            ? "Sente360"
            : workshopName.Trim();

        var html = BuildInviteHtml(createdUser, setupUrl, brandName);

        return await _emailSender.SendAsync(new EmailMessageDto
        {
            ToEmail = createdUser.Email,
            ToName = createdUser.FullName,
            Subject = "Sente360 hesabınızı oluşturun",
            HtmlBody = html,
            TextBody = $"Sente360 hesabınızı oluşturmak için bağlantı: {setupUrl}"
        });
    }

    private static string BuildInviteHtml(
        AdminWorkshopUserCreatedViewModel user,
        string setupUrl,
        string brandName)
    {
        var safeName = WebUtility.HtmlEncode(user.FullName);
        var safeUserName = WebUtility.HtmlEncode(user.UserName);
        var safeBrandName = WebUtility.HtmlEncode(brandName);
        var safeUrl = WebUtility.HtmlEncode(setupUrl);
        var safeCode = WebUtility.HtmlEncode(user.PasswordSetupCode);

        return $@"
<!doctype html>
<html lang='tr'>
<head><meta charset='utf-8'></head>
<body style='margin:0;background:#f6f8fb;font-family:Arial,Helvetica,sans-serif;color:#0f172a'>
    <div style='max-width:640px;margin:0 auto;padding:28px'>
        <div style='background:white;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden'>
            <div style='padding:24px 28px;background:#4f35e8;color:white'>
                <h1 style='margin:0;font-size:24px'>Sente360</h1>
                <p style='margin:8px 0 0;opacity:.9'>{safeBrandName} hesabına davet edildiniz</p>
            </div>
            <div style='padding:28px'>
                <p style='font-size:16px;line-height:1.6;margin-top:0'>Merhaba <strong>{safeName}</strong>,</p>
                <p style='font-size:16px;line-height:1.6'>Sente360 hesabınız oluşturuldu. Güvenliğiniz için şifrenizi siz belirleyeceksiniz.</p>

                <div style='background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:16px;margin:18px 0'>
                    <small style='color:#64748b;font-weight:bold'>Kullanıcı Adı</small><br>
                    <strong>{safeUserName}</strong>
                </div>

                <a href='{safeUrl}' style='display:inline-block;background:#4f35e8;color:white;text-decoration:none;border-radius:12px;padding:14px 22px;font-weight:bold'>Şifremi Belirle</a>

                <p style='margin-top:22px;color:#64748b;font-size:13px;line-height:1.5'>
                    Buton çalışmazsa aşağıdaki bağlantıyı tarayıcınıza yapıştırabilirsiniz:<br>
                    <span style='word-break:break-all'>{safeUrl}</span>
                </p>

                <p style='margin-top:18px;color:#64748b;font-size:13px;line-height:1.5'>
                    Davet kodu ile giriş yapmak isterseniz kodunuz: <strong>{safeCode}</strong>
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
