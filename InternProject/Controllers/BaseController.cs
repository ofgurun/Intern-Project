using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var username = context.HttpContext.Session.GetString("Username");
        var action = (context.ActionDescriptor.RouteValues["action"] ?? "").ToLower();

        // Login ve Register sayfaları için kontrol yapma
        if (action == "login" || action == "register")
        {
            base.OnActionExecuting(context);
            return;
        }

        // Giriş yapılmamışsa login sayfasına yönlendir
        if (string.IsNullOrEmpty(username))
        {
            context.Result = new RedirectToActionResult("Login", "Home", null);
            return;
        }

        // Artık rol kontrolü yok, giriş yapan herkes her sayfaya erişebilir
        base.OnActionExecuting(context);
    }
}
