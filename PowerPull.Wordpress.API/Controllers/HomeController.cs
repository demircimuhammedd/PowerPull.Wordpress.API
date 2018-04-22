using System.Web.Mvc;

namespace PowerPull.Wordpress.API.Controllers
{
  public class HomeController : Controller
  {
    public ActionResult Index()
    {
      return Redirect("/swagger");
    }
  }
}
