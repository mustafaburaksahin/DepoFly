// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using DepoFly.Models; // ApplicationUser için şart aga

namespace DepoFly.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
    
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Lütfen e-posta adresinizi girin.")]
            [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
      
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    // Güvenlik gereği kullanıcı yoksa bile "Gönderdik" sayfasına atıyoruz
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // MAİL İÇERİĞİ TÜRKÇE VE ŞIK
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "DepoFly Şifre Sıfırlama",
                    $"<h2 style='color:#0d6efd;'>DepoFly Panel</h2><p>Şifreni sıfırlamak için <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>buraya tıkla.</a>.</p>");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}