using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Website.Helper;

namespace Website.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebcamUploadController : ControllerBase
    {
        #region Constants

        private const string ISO_8859_1_ENCODING = "ISO-8859-1";
        private const int PNG_MAGIC_NUMBER_BYTE_COUNT = 8;
        private const string PNG_MAGIC_NUMBER = "89-50-4E-47-0D-0A-1A-0A";
        private const string IMAGES_FOLDER = "images";

        #endregion Constants

        #region Private variables

        private readonly ILogger _logger;

        #endregion Private variables

        public WebcamUploadController(ILogger<WebcamUploadController> logger)
        {
            _logger = logger;
        }


        #region API methods

        [Consumes(MediaTypeNames.Application.Octet)]
        [RequestSizeLimit(25000)]
        public StatusCodeResult Post()
        {
            //If authorization header was not valid or could not be authenticated
            if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out AuthenticationHeaderValue authentication) || !IsBasicAuthenticated(authentication))
            {
                _logger.LogInformation("Authentication was not passed", null);
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            DateTime accessDateTime = DateTime.Now;

            //Read request body as a png byte array
            byte[] pngBytes;
            try
            {
                pngBytes = ReadBodyAsPngBytes();
            }
            catch
            {
                _logger.LogInformation("Request body was not valid PNG byte array", null);
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("All pre-conditions passed, request is valid to process", null);

            //Save PNG to file
            try
            {   
                SavePngBytesToFile(pngBytes, accessDateTime);
            }
            catch
            {
                _logger.LogInformation("Error while processing request", null);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            //All was successful!
            _logger.LogInformation("Request was successfully processed", null);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        #endregion API methods

        #region Helper methods

        /// <summary>
        /// Validates headers with basic auth
        /// </summary>
        /// <param name="authentication">AuthenticationHeaderValue</param>
        /// <returns>True if authenticated</returns>
        private bool IsBasicAuthenticated(AuthenticationHeaderValue authentication)
        {
            bool isAuthenticated = false;

            if (authentication.Scheme == AuthenticationSchemes.Basic.ToString() && authentication.Parameter != null)
            {
                string username, password;
                try
                {
                    string[] credentials = Encoding.GetEncoding(ISO_8859_1_ENCODING).GetString(Convert.FromBase64String(authentication.Parameter)).Split(':');
                    username = credentials[0];
                    password = credentials[1];
                    _logger.LogDebug("Credentials loaded from authenitcation headers", new { username, password });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{nameof(IsBasicAuthenticated)} will return false (invalid).  Warning error: {ex.Message}", ex);
                    return false;
                }

                isAuthenticated = username == AppSettings.ApiUsername && password == AppSettings.ApiPassword;
                _logger.LogDebug($"Credentials are {(isAuthenticated ? "" : "in")}valid", isAuthenticated);
            }

            return isAuthenticated;
        }

        /// <summary>
        /// Reads the request body into a PNG byte array
        /// </summary>
        /// <returns></returns>
        private byte[] ReadBodyAsPngBytes()
        {
            byte[] pngBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                Request.Body.CopyTo(ms);
                pngBytes = ms.ToArray();
            }
            _logger.LogDebug($"Body copied to {nameof(pngBytes)}", pngBytes.Length);

            // Validate byte array is in fact png with PNG_MAGIC_NUMBER
            if (BitConverter.ToString(pngBytes.Take(PNG_MAGIC_NUMBER_BYTE_COUNT).ToArray()) != PNG_MAGIC_NUMBER)
            {
                _logger.LogWarning($"{nameof(pngBytes)} does not pass magic number validatiorn", PNG_MAGIC_NUMBER);
                throw new FormatException();
            }

            return pngBytes;
        }

        /// <summary>
        /// Writes a PNG byte array to a file
        /// </summary>
        /// <param name="pngBytes">PNG byte array</param>
        /// <param name="accessDateTime">Access DateTime</param>
        /// <returns>PNG file path</returns>
        private string SavePngBytesToFile(byte[] pngBytes, DateTime accessDateTime)
        {
            string pngFilePath = string.Empty;

            try
            {
                pngFilePath = GetWebcamUploadFullFilePath(accessDateTime);
                using FileStream fs = new FileStream(pngFilePath, FileMode.Create, FileAccess.Write);
                fs.Write(pngBytes, 0, pngBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(SavePngBytesToFile)} error: {ex.Message}", new { ex, pngFilePath });
                throw;
            }

            _logger.LogDebug("PNG file saved successfully", pngFilePath);
            return pngFilePath;
        }

        /// <summary>
        /// Gets the full file path for a PNG webcame upload
        /// </summary>
        /// <param name="accessDateTime">DateTime upload was accessed</param>
        /// <returns>Webcam upload PNG full file path</returns>
        private string GetWebcamUploadFullFilePath(DateTime accessDateTime)
        {
            return $@"{AppSettings.RootFolder}\{GetWebcamUploadFolderPath()}\{accessDateTime:yyyy-MM-dd-HH-mm-ss-fff}.{ImageFormat.Png}";
        }

        /// <summary>
        /// Gets the folder path from wwwroot for a webcam upload
        /// </summary>
        /// <returns>Folder path</returns>
        private static string GetWebcamUploadFolderPath()
        {
            return $@"{IMAGES_FOLDER}\{nameof(WebcamUploadController)}";
        }

        /// <summary>
        /// Ges the latest PNG file url in the webcam upload root folder.
        /// Latest file is determined by name descending and Url path is relative to wwwroot
        /// </summary>
        /// <returns>Latest PNG file Url relative to wwwroot</returns>
        public static string GetWebcamUploadLatestUrl()
        {
            try
            {
                string latestPngFilePath = Directory.GetFiles($@"{AppSettings.RootFolder}\{GetWebcamUploadFolderPath()}", $"*.{ImageFormat.Png}").ToList().OrderByDescending(x => x).FirstOrDefault();
                return latestPngFilePath.Replace(AppSettings.RootFolder, "");
            }
            catch
            {
                throw;
            }
        }

        #endregion Helper methods
    }
}
