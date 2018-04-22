using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;
using WordPressSharp;
using WordPressSharp.Models;

namespace PowerPull.Wordpress.API.Controllers
{
  public class WordpressController : ApiController
  {
    private string _ConnectionString
    {
      get
      {
        return "Server = 94.73.147.204; Database = u7614282_bigdatadb; Uid = u7614282_user972; Pwd = ESyy30R7; Convert Zero Datetime = true; Allow User Variables = True; ";
      }
    }

    public async Task<string> Get()
    {
      return "Success";
    }


    public async Task PostAsync([FromBody]ParamObj paramObj)
    {
      await SaveBlogAsync(paramObj);
    }

    public static async Task SaveBlogAsync(ParamObj postsTemp)
    {
      Console.WriteLine(
          "Makale Seo uyumlu olsun mu?: \r\n " +
             "1- Evet \r\n " +
             "2- Hayır");

      bool isSeoArticle = false;// Convert.ToInt32(Console.ReadLine()) == 1 ? true : false;

      using (MySqlConnection mySqlConnection = new MySqlConnection("Server = 94.73.147.204; Database = u7614282_mzills; Uid = u7614282_mzills; Pwd = FEax10F8; Convert Zero Datetime = true; Allow User Variables=True; "))
      {
        WordPressSiteConfig config = new WordPressSiteConfig
        {
          BaseUrl = postsTemp.Website,
          Username = postsTemp.Username,
          Password = postsTemp.Password,
          BlogId = 1
        };

        using (var client = new WordPressClient(config))
        {
          int index = postsTemp.Posts.Count;
          foreach (var item in postsTemp.Posts)
          {
            try
            {

              if (!IsExistRowDb(mySqlConnection, item.Title))
              {
                string contentBody = string.Empty;
                if (isSeoArticle)
                {
                  contentBody = ArticleReWrite(WebUtility.HtmlDecode(item.Content.Replace("'", "")).Replace("'", "").Replace("“", "").Replace("”", "")).Replace("?", "");
                }
                string seoBody = WebUtility.HtmlDecode(isSeoArticle ? (contentBody.Length > 50) ? contentBody : item.Content : item.Content);
                if (string.IsNullOrEmpty(seoBody)) continue;

                item.SeoBody = seoBody;

                item.Title = WebUtility.HtmlDecode(item.Title);
                var post = new Post
                {
                  PostType = "post",
                  Title = item.Title,
                  Content = item.SeoBody,
                  PublishDateTime = DateTime.Now,
                  Status = "publish"
                };

                try
                {
                  if (!item.Media.Contains("iframe") && !string.IsNullOrEmpty(item.Media))
                  {
                    var media = XElement.Parse(item.Media).FirstAttribute.Value;

                    string imgExtension = string.Empty;
                    if (media.Contains(".jpg"))
                      imgExtension = ".jpg";

                    if (media.Contains(".png"))
                      imgExtension = ".png";

                    var featureImage = Data.CreateFromUrl(media.Substring(0, media.IndexOf(imgExtension) + imgExtension.Length));
                    UploadResult uploadResult = await client.UploadFileAsync(featureImage);
                    post.FeaturedImageId = uploadResult.Id;
                  }
                  else
                  {
                    post.Content = string.Concat(post.Content, item.Media);
                    post.FeaturedImageId = "1";
                  }
                }
                catch (Exception)
                {
                  post.Content = string.Concat(post.Content, item.Media);
                  post.FeaturedImageId = "1";
                }
                 
                await client.NewPostAsync(post);

                Console.WriteLine("({0} - {1}) Eklenen makale: {2}", postsTemp.Posts.Count, index, item.Title);
              }
              else
              {
                Console.WriteLine("({0} - {1}) Zaten kayıtlı makale: {2}", postsTemp.Posts.Count, index, item.Title);
              }

              index--;
            }
            catch (Exception ex)
            {
              Console.WriteLine(ex.Message);
              continue;
            }
          }
        }
      }
    }

    private static bool IsExistRowDb(MySqlConnection mySqlConnection, string post_title)
    {
      return false;
      //var total = mySqlConnection.Query<long>("SELECT COUNT(*) FROM wp_posts where post_title = @post_title", new { post_title }).Single();
      //if (total > 0) return true;
      //else return false;
    }

    private static string ArticleReWrite(string article)
    {
      string api_key = "e1fa82649659a96614ea87b790cac4b1";
      string serviceUri = "http://www.re-writer.net/api.php";
      string lang = "en";

      string post_data = "api_key=" + api_key + "&article=" + article + "&lang=" + lang;

      HttpWebRequest request = (HttpWebRequest)
      WebRequest.Create(serviceUri);
      request.Method = "POST";

      byte[] postBytes = System.Text.Encoding.ASCII.GetBytes(post_data);
      request.ContentType = "application/x-www-form-urlencoded";
      request.ContentLength = postBytes.Length;
      Stream requestStream = request.GetRequestStream();

      requestStream.Write(postBytes, 0, postBytes.Length);
      requestStream.Close();

      HttpWebResponse response = (HttpWebResponse)request.GetResponse();
      return new StreamReader(response.GetResponseStream()).ReadToEnd();
    }
  }

  public class PostTemp
  {
    public string Title { get; set; }
    public string Spot { get; set; }
    public string Url { get; set; }
    public string Media { get; set; }
    public string Content { get; set; }
    public string SeoBody { get; set; }
  }

  public class ParamObj
  {
    public List<PostTemp> Posts { get; set; }
    public string Website { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
  }
}