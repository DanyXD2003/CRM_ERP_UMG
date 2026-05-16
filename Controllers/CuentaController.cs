using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CRM_ERP_UMG.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<UsuarioAplicacion> administradorSesion;

        public CuentaController(SignInManager<UsuarioAplicacion> administradorSesion)
        {
            this.administradorSesion = administradorSesion;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVista { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVista modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await administradorSesion.PasswordSignInAsync(
                modelo.Email,
                modelo.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (resultado.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
                {
                    return Redirect(modelo.ReturnUrl);
                }

                return RedirectToAction("Index", "Modulos");
            }

            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(modelo);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await administradorSesion.SignOutAsync();
            return RedirectToAction("Login", "Cuenta");
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
