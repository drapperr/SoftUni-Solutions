﻿namespace SIS.WebServer
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Api;
    using HTTP.Cookies;
    using HTTP.Enums;
    using HTTP.Exceptions;
    using HTTP.Requests;
    using HTTP.Responses;
    using HTTP.Sessions;
    using Results;

    public class ConnectionHandler
    {
        private readonly Socket client;

        private readonly IHttpHandler handler;

        public ConnectionHandler(Socket client, IHttpHandler handler)
        {
            this.client = client;
            this.handler = handler;
        }

        private async Task<IHttpRequest> ReadRequest()
        {
            var result = new StringBuilder();

            var data = new ArraySegment<byte>(new byte[1024]);

            while (true)
            {
                int bytesRead = await this.client.ReceiveAsync(data.Array, SocketFlags.None);

                if (bytesRead == 0)
                {
                    break;
                }

                var bytesAsString = Encoding.UTF8.GetString(data.Array, 0, bytesRead);
                result.Append(bytesAsString);

                if (bytesRead < 1023)
                {
                    break;
                }
            }

            if (result.Length == 0)
            {
                return null;
            }

            return new HttpRequest(result.ToString());
        }

        private IHttpResponse HandleRequest(IHttpRequest httpRequest)
        {
            var response = this.handler.Handle(httpRequest);

            if (response == null)
            {
                response = this.ReturnIfResource(httpRequest.Path);
            }

            return response;
        }

        private IHttpResponse ReturnIfResource(string httpRequestPath)
        {
            string path = "Resources/" + httpRequestPath;
            
            if (path.Contains("..") || !File.Exists(path))
            {
                return new HtmlResult("<h1>404 Not Found</h1>", HttpResponseStatusCode.NotFound);
            }

            byte[] content = File.ReadAllBytes(path);
            
            return new InlineResourceResult(content, HttpResponseStatusCode.Ok);
        }

        private string SetRequestSession(IHttpRequest httpRequest)
        {
            string sessionId;

            if (httpRequest.Cookies.ContainsCookie(HttpSessionStorage.SessionCookieKey))
            {
                var cookie = httpRequest.Cookies.GetCookie(HttpSessionStorage.SessionCookieKey);
                sessionId = cookie.Value;
            }
            else
            {
                sessionId = Guid.NewGuid().ToString();
            }
            
            httpRequest.Session = HttpSessionStorage.GetSession(sessionId);

            return sessionId;
        }

        private async Task PrepareResponse(IHttpResponse httpResponse)
        {
            byte[] byteSegments = httpResponse.GetBytes();

            await this.client.SendAsync(byteSegments, SocketFlags.None);
        }

        public async Task ProcessRequestAsync()
        {
            try
            {
                var httpRequest = await this.ReadRequest();

                if (httpRequest != null)
                {
                    string sessionId = this.SetRequestSession(httpRequest);
                    
                    var httpResponse = this.HandleRequest(httpRequest);

                    this.SetResponseSession(httpResponse, sessionId);
                    
                    await this.PrepareResponse(httpResponse);
                }
            }
            catch (BadRequestException e)
            {
                await this.PrepareResponse(new HtmlResult($"<h1>400 Bad Request</h1><p>{e.Message}</p>",
                    HttpResponseStatusCode.BadRequest));
            }
            catch (Exception e)
            {
                await this.PrepareResponse(new HtmlResult($"<h1>500 Internal Server Error</h1><p>{e.Message}</p>",
                    HttpResponseStatusCode.InternalServerError));
            }

            this.client.Shutdown(SocketShutdown.Both);
        }

        private void SetResponseSession(IHttpResponse httpResponse, string sessionId)
        {
            if (sessionId != null)
            {
                httpResponse.Cookies.Add(new HttpCookie(HttpSessionStorage.SessionCookieKey, sessionId));
            }
        }
    }
}