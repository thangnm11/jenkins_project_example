using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace OSMproxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OSMController : ControllerBase
    {
        private readonly ILogger<OSMController> _logger;
        private readonly IConfiguration _config;

        public OSMController(ILogger<OSMController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet("tile/{z}/{x}/{y}")]
        public IActionResult GetTile([Required] int z, [Required] int x, [Required] int y)
        {
            return GetTile("null", z, x, y, "png");
        }

        [HttpGet("tile/{z}/{x}/{y}.{png}")]
        public IActionResult GetTile([Required] int z, [Required] int x, [Required] int y, [Required] String png)
        {
            return GetTile("null", z, x, y, png);
        }

        [HttpGet("tile/{s}/{z}/{x}/{y}.{png}")]
        public IActionResult GetTile([Required(AllowEmptyStrings = true)] String s, [Required] int z, [Required] int x, [Required] int y, [Required] String png = "png")
        {
            try
            {
                Boolean isAutoSaveMap = _config.GetValue<Boolean>("isAutoSaveMap");
                Boolean isAutoUpdateMap = _config.GetValue<Boolean>("isAutoUpdateMap");
                Boolean isUseOnlineMap = _config.GetValue<Boolean>("isUseOnlineMap");
                String sPathSaveMap = _config.GetValue<String>("sPathSaveMap");
                String[] arrTileServer = _config.GetSection("arrTileServer").Get<String[]>();

                s = String.IsNullOrEmpty(s) ? "null" : s;
                String sFileName = sPathSaveMap + String.Format("{0}/{1}/{2}/{3}.{4}", s, z, x, y, png);

                if (!isUseOnlineMap)
                    return PhysicalFile(sFileName, "image/png", String.Format("{0}.{1}", y, png));

                if (arrTileServer == null || arrTileServer.Length < 1)
                    throw new Exception("arrTileServer not found in config.");
                foreach (String sTileServer in arrTileServer)
                {
                    RestRequest restRequest = new RestRequest(Method.GET);
                    restRequest.RequestFormat = DataFormat.Json;
                    RestClient restClient = new RestClient(String.Format(sTileServer, s, z, x, y));
                    IRestResponse restResponse = restClient.Get(restRequest);
                    if (!restResponse.IsSuccessful)
                        continue;
                    if (isAutoSaveMap)
                    {
                        if (!System.IO.File.Exists(sFileName) || isAutoUpdateMap)
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(sFileName)))
                                Directory.CreateDirectory(Path.GetDirectoryName(sFileName));
                            System.IO.File.WriteAllBytes(sFileName, restResponse.RawBytes);
                        }
                    }
                    return File(new MemoryStream(restResponse.RawBytes), "image/png", String.Format("{0}.{1}", y, png));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
            throw new FileNotFoundException();
        }

        [HttpGet("router")]
        public IActionResult GetRouter([Required] String api)
        {
            //    Dim r As HttpResponse = context.Response
            //r.ContentType = "application/x-javascript; charset=UTF-8"
            String sTileServer = "";
            switch (api)
            {
                case "osm":
                    sTileServer = "http://router.project-osrm.org/viaroute" + Request.QueryString.Value.Replace("&api=osm", "");
                    break;

                case "nominatim":
                    sTileServer = "http://nominatim.openstreetmap.org/search/" + Request.QueryString.Value.Replace("&api=nominatim", "");
                    break;

                case "bing":
                    sTileServer = "http://dev.virtualearth.net/REST/v1/Locations" + Request.QueryString.Value.Replace("&api=bing", "");
                    break;

                case "google":
                    sTileServer = "https://maps.googleapis.com/maps/api/geocode/json" + Request.QueryString.Value.Replace("&api=google", "");
                    break;

                default:
                    throw new KeyNotFoundException();
            }
            RestRequest restRequest = new RestRequest(Method.GET);
            restRequest.RequestFormat = DataFormat.Json;
            RestClient restClient = new RestClient(sTileServer);
            IRestResponse restResponse = restClient.Get(restRequest);
            if (!restResponse.IsSuccessful)
            {
                if (restResponse.ErrorException == null)
                {
                    //HttpResponseMessage httpResponseMessage =new HttpResponseMessage(restResponse.StatusCode);
                    //httpResponseMessage.Content = restResponse.ErrorMessage;
                    return Problem(restResponse.Content, null, restResponse.StatusCode.GetHashCode());
                    //throw new HttpRequestException(restResponse.Content);
                }
                else
                    throw restResponse.ErrorException;
            }
            //continue;


            return Ok(restResponse.Content);
        }
    }
}