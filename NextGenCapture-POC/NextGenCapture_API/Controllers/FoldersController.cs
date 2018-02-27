using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using NextGenCapture_Common.Model.Export;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NextGenCapture_Common.Model.APIGateway;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NextGenCapture_RabbitMQService.Model;

namespace NextGenCapture_API.Controllers
{
    [DisableFormValueModelBindingAttribute]
    [Route("api/Folders")]
    public class FoldersController : Controller
    {
        private IConfiguration _configuration;

        public FoldersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("{folderName}")]
        public async Task<HttpResponseMessage> PostFiles([FromForm] string value)
        {
            FormOptions _defaultFormOptions = new FormOptions();

            DateTime methodStartTimeStamp = DateTime.Now;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Created);

            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                response = GetResponseMessage(HttpStatusCode.BadRequest, MessageConstant.MimePartErrorMessage);
                return response;
            }

            var formAccumulator = new KeyValueAccumulator();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();

            string targetFilePath = string.Empty;

            var attachmentList = new List<Attachment>();

            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        var rootPath = _configuration["SharedLocation:RootPath"].ToString();
                        try
                        {
                            var directoryInfo = Directory.CreateDirectory(Path.Combine(rootPath, Guid.NewGuid().ToString()));

                            targetFilePath = directoryInfo.FullName + "//" + section.AsFileSection().FileName;
                            var bodyStream = new MemoryStream();
                            section.Body.CopyTo(bodyStream);
                            byte[] attachmentData = new byte[bodyStream.Length];
                            attachmentList.Add(new Attachment()
                            {
                                FileName = section.AsFileSection().FileName,
                                FileData = bodyStream.ToArray()
                            });

                            using (var targetStream = System.IO.File.Create(targetFilePath))
                            {
                                bodyStream.Position = 0;
                                await bodyStream.CopyToAsync(targetStream);
                            }

                            bodyStream.Close();
                        }
                        catch (Exception ex)
                        {
                            response = GetResponseMessage(HttpStatusCode.BadRequest, ex.Message);
                            return response;
                        }

                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var attachmentValue = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.ToString(), attachmentValue);

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                response = GetResponseMessage(HttpStatusCode.BadRequest, MessageConstant.KeyCountLimit);
                                return response;
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            if(formAccumulator.GetResults().Count<0)
            {
                response = GetResponseMessage(HttpStatusCode.BadRequest, MessageConstant.NoMetaData);
                return response;
            }
            else if(formAccumulator.GetResults().Count > 1)
            {
                response = GetResponseMessage(HttpStatusCode.BadRequest, MessageConstant.MoreThanOneMetaData);
                return response;
            }
            else
            {
                var batchMetaDataDictionary = formAccumulator.GetResults().FirstOrDefault();
                if(batchMetaDataDictionary.Key!="batchMetaData")
                {
                    response = GetResponseMessage(HttpStatusCode.BadRequest, MessageConstant.NoMetaData);
                    return response;
                }
                else
                {
                    try
                    {
                        var settings = new JsonSerializerSettings
                        {
                            NullValueHandling=NullValueHandling.Ignore,
                            MissingMemberHandling=MissingMemberHandling.Ignore
                        };

                        var captureMetaData = JsonConvert.DeserializeObject<ApiMetaData>(batchMetaDataDictionary.Value, settings);

                        STPBatch batch = new STPBatch()
                        {
                            Destination = "MyQ",
                            Attachments = attachmentList,
                            BatchMetaData = captureMetaData,
                            FolderName = Guid.NewGuid().ToString(),
                            ReceivedDateTime = DateTime.Now,
                            Status = "Ready"
                        };


                        var rabbitMqServiceInstance= ServiceProxy.Create<IMQService>(new Uri("fabric:/NextGenCapture_POC/NextGenCapture_RabbitMQService"));

                        await rabbitMqServiceInstance.SendToRabbitMessageQueue(batch);
                    }
                    catch(Exception ex)
                    {
                        response = GetResponseMessage(HttpStatusCode.BadRequest, ex.Message);
                        return response;
                    }
                }
            }


            return response;

        }

        private HttpResponseMessage GetResponseMessage(HttpStatusCode statusCode,string message)
        {
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            response.ReasonPhrase = message;

            return response;
        }

        private Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

    }
}