﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using cimobgrupo2.Models;
using cimobgrupo2.Models.ManageViewModels;
using cimobgrupo2.Services;
using cimobgrupo2.Data;

namespace cimobgrupo2.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly List<Ajuda> _ajudas;
        private readonly List<Erro> _erros;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _ajudas = context.Ajudas.Where(ai => ai.Controller == "Manage").ToList();
            _erros = context.Erros.ToList();
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetHelpModal();
            SetHelpToolTips();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new IndexViewModel();

            model.ChangeDetails = new ChangeDetailsViewModel
            {
                Nome = user.Nome,
                DataNascimento = user.DataNascimento,
                Email = user.Email,
                Contato = user.Contato
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeDetails(IndexViewModel model)
        {
            SetHelpModal();
            SetHelpToolTips();

            if (!ModelState.IsValid)
            {
                SetErrorMessage("003");
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (model.ChangeDetails.Email != null)
                user.Email = model.ChangeDetails.Email;

            if (model.ChangeDetails.Nome != null)
                user.Nome = model.ChangeDetails.Nome;

            if (model.ChangeDetails.DataNascimento != null)
                user.DataNascimento = model.ChangeDetails.DataNascimento;

            if(model.ChangeDetails.Contato != null)
                user.Contato = model.ChangeDetails.Contato;

            await _userManager.UpdateAsync(user);

            TempData["result"] = "success";
            StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(IndexViewModel model)
        {
            SetHelpModal();
            SetHelpToolTips();

            var user = await _userManager.GetUserAsync(User);
            model.ChangeDetails = new ChangeDetailsViewModel
            {
                Nome = user.Nome,
                DataNascimento = user.DataNascimento,
                Email = user.Email,
                Contato = user.Contato
            };


            if (!ModelState.IsValid)
            {
                SetErrorMessage("003");
                return View("Index", model);
            }
            
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.ChangePassword.OldPassword, model.ChangePassword.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View("Index", model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            TempData["result"] = "success";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(IndexViewModel model)
        {
            SetHelpModal();
            SetHelpToolTips();

            var user = await _userManager.GetUserAsync(User);
            model.ChangeDetails = new ChangeDetailsViewModel
            {
                Nome = user.Nome,
                DataNascimento = user.DataNascimento,
                Email = user.Email,
                Contato = user.Contato
            };


            if (!ModelState.IsValid)
            {
                SetErrorMessage("003");
                return View("Index", model);
            }

            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (await _userManager.CheckPasswordAsync(user, model.DeleteAccount.Password))
                await _userManager.DeleteAsync(user);
            else
                return RedirectToAction("Index", model);
            
            TempData["Message"] = "Conta eliminada.";
            return RedirectToAction("Login", "Account");
        }

        private void SetErrorMessage(String Code)
        {
            var Erro = _erros.SingleOrDefault(e => e.Codigo == Code);
            ViewData["Error_Code"] = Erro.Codigo;
            ViewData["Error_Message"] = Erro.Mensagem;
        }

        #region SetHelp
        private void SetHelpToolTips()
        {
            ViewData["Nome"] = _ajudas.Single(ai => ai.Action == "ChangeDetails" && ai.Elemento == "Nome").Texto;
            ViewData["DataNascimentoPicker"] = _ajudas.Single(ai => ai.Action == "ChangeDetails" && ai.Elemento == "DataNascimentoPicker").Texto;
            ViewData["Email"] = _ajudas.Single(ai => ai.Action == "ChangeDetails" && ai.Elemento == "Email").Texto;
            ViewData["Contato"] = _ajudas.Single(ai => ai.Action == "ChangeDetails" && ai.Elemento == "Contato").Texto;

            ViewData["PasswordAntiga"] = _ajudas.Single(ai => ai.Action == "ChangePassword" && ai.Elemento == "PasswordAntiga").Texto;
            ViewData["NovaPassword"] = _ajudas.Single(ai => ai.Action == "ChangePassword" && ai.Elemento == "NovaPassword").Texto;
            ViewData["ConfirmarNovaPassword"] = _ajudas.Single(ai => ai.Action == "ChangePassword" && ai.Elemento == "ConfirmarNovaPassword").Texto;

            ViewData["PasswordAtual"] = _ajudas.Single(ai => ai.Action == "DeleteAccount" && ai.Elemento == "PasswordAtual").Texto;
        }

        private void SetHelpModal()
        {
            ViewData["TituloModalAjuda"] = "Definições de Conta";
            ViewData["TextoModalAjuda"] = _ajudas.Single(ai => ai.Action == "Index").Texto;
        }

        #endregion

        #region Helpers

        private void AddErrors(IdentityResult result)
            {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            }

        #endregion
    }
}