using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PowerPull.Wordpress.API.Controllers.Base;
using RestSharp;
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
  public class WordpressController : BaseController
  { 
    public string Get => "Success";

    public async Task PostAsync([FromBody]FireViewModel fire)
    {
      RestClient restClient = new RestClient(ApiUrl);
      RestRequest request = new RestRequest("wordpress/{id}", Method.GET);
      request.AddUrlSegment("id", fire.Id);
      IRestResponse restResponse = await restClient.ExecuteTaskAsync<dynamic>(request);
      Wordpress wordpress = JsonConvert.DeserializeObject<Wordpress>(restResponse.Content);

      string connectionString =
        string.Format("Server = {0}; Database = {1}; Uid = {2}; Pwd = {3}; Convert Zero Datetime = true; Allow User Variables=True; ",
        wordpress.Server, wordpress.Database_Name, wordpress.UId, wordpress.Pwd);

      using (MySqlConnection _connection = new MySqlConnection(connectionString))
      {
        bool isSeoArticle = false;
        WordPressSiteConfig config = new WordPressSiteConfig { BaseUrl = wordpress.Website, Username = wordpress.Username, Password = wordpress.Password, BlogId = 1 };

        using (WordPressClient client = new WordPressClient(config))
        {
          int index = fire.List.Count;
          foreach (var item in fire.List)
          {
            try
            {
              if (!IsExistRowDb(_connection, item.Title))
              {
                string contentBody = string.Empty;
                if (isSeoArticle)
                {
                  contentBody = ArticleReWrite(WebUtility.HtmlDecode(item.Content.Replace("'", "")).Replace("'", "").Replace("“", "").Replace("”", "")).Replace("?", "");
                }
                string seoBody = WebUtility.HtmlDecode(isSeoArticle ? (contentBody.Length > 50) ? contentBody : item.Content : item.Content);
                if (string.IsNullOrEmpty(seoBody)) continue;

                var post = new Post
                {
                  PostType = "post",
                  Title = item.Title,
                  Content = seoBody,
                  PublishDateTime = DateTime.Now,
                  Status = "publish"
                };

                post = await MediaProcessAsync(client, post, item.Media);
                await client.NewPostAsync(post);
                Console.WriteLine("({0} - {1}) Eklenen makale: {2}", fire.List.Count, index, item.Title);
              }
              else
              {
                Console.WriteLine("({0} - {1}) Zaten kayıtlı makale: {2}", fire.List.Count, index, item.Title);
              }

              index--;
            }
            catch
            {
              continue;
            }
          }
        }
      }
    }

    private async Task<Post> MediaProcessAsync(WordPressClient client, Post post, string media)
    {
      try
      {
        if (!media.Contains("iframe") && !string.IsNullOrEmpty(media))
        {
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
          post.Content = string.Concat(post.Content, media);
          post.FeaturedImageId = "1";
        }
      }
      catch (Exception)
      {
        post.Content = string.Concat(post.Content, media);
        post.FeaturedImageId = "1";
      }

      return post;
    }

    private static bool IsExistRowDb(MySqlConnection connection, string post_title)
    {
      var total = connection.Query<long>("SELECT COUNT(*) FROM wp_posts where post_title = @post_title", new { post_title }).Single();
      if (total > 0) return true;
      else return false;
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

  public class FireViewModel
  {
    public int? Id { get; set; }
    public List<Fire> List { get; set; }
  }

  public class Fire
  {
    public string Url { get; set; }
    public string Title { get; set; }
    public string Spot { get; set; }
    public string Media { get; set; }
    public string Content { get; set; }
  }

  public class Wordpress
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public string Website { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Server { get; set; }
    public string Database_Name { get; set; }
    public string UId { get; set; }
    public string Pwd { get; set; }
  }
}