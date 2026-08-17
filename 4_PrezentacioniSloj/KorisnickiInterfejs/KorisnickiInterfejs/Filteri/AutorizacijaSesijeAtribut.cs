using System.Web;
using System.Web.Mvc;

namespace SportskiKlub.Filteri
{
    public class AutorizacijaSesijeAtribut : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return httpContext.Session["IDKorisnika"] != null;
        }

        protected override void HandleUnauthorizedRequest(
            AuthorizationContext filterContext)
        {
            filterContext.Result =
                new RedirectResult("/Nalog/Prijava");
        }
    }
}
