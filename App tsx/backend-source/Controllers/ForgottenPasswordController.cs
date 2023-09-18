using System.Security.Policy;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using OperationCHAN.Areas.Identity.Services;
using OperationCHAN.Models;
using Org.BouncyCastle.Asn1.Ocsp;

namespace OperationCHAN.Controllers;

public class ForgottenPasswordController
{
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUrlHelper _URL;
    public ForgottenPasswordController(IEmailSender emailSender, 
        UserManager<ApplicationUser> userManager, IUrlHelper URL)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _URL = URL;
    }
    public async void HandleForgottenPassword(ApplicationUser user)
    {
        string message = "";
        // For more information on how to enable account confirmation and password reset please
        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var url = "https://chanv2.duckdns.org:7006/Account/ResetPassword?code=" + code;

        await _emailSender.SendEmailAsync(
            user.Email,
            "Reset Password",
            $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(url)}'>clicking here</a>.");
    }
}