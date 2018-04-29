using System.Web.Http;

namespace PowerPull.Wordpress.API.Controllers.Base
{
  public class BaseController : ApiController
  {
    public string ApiUrl
    {
      get
      {
        return "http://powerpullapi.azurewebsites.net/api/";
      }
    }

    public string ApiWordpressUrl
    {
      get
      {
        return "http://powerpullwordpressapi.azurewebsites.net/api/";
      }
    }
  }
}