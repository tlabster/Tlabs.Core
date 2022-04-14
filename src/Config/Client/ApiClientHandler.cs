using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config.Client {

  ///<summary>Web API client handler to log requests.</summary>
  public class ApiClientHandler : HttpClientHandler {
    static readonly ILogger<ApiClientHandler> log= App.Logger<ApiClientHandler>();

    ///<inherit/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage reqMsg, System.Threading.CancellationToken cancellationToken) {
      var watch= Misc.TimingWatch.StartTiming();
      logReqStart(log, reqMsg.Method, reqMsg.RequestUri, null);

      var respMsg=  await base.SendAsync(reqMsg, cancellationToken);
      var contHdr= respMsg.Content.Headers;
      
      logReqEnd(log, reqMsg.Method, reqMsg.RequestUri, respMsg.StatusCode, contHdr.ContentType, contHdr.ContentLength, watch.GetElapsedTime().TotalMilliseconds, null);

      return respMsg;
    }

    static readonly EventId ReqStartId= new EventId(1001000, nameof(ApiClientHandler) + "-Start");
    static readonly EventId ReqEndId= new EventId(1001001, nameof(ApiClientHandler) + "-End");

    private static readonly Action<ILogger, HttpMethod, Uri, Exception> logReqStart=
      LoggerMessage.Define<HttpMethod, Uri>(LogLevel.Information, ReqStartId, "{Method}: {Uri}");

    private static readonly Action<ILogger, HttpMethod, Uri, HttpStatusCode, MediaTypeHeaderValue, long?, double, Exception> logReqEnd=
      LoggerMessage.Define<HttpMethod, Uri, HttpStatusCode, MediaTypeHeaderValue, long?, double>(
        LogLevel.Information,
        ReqEndId,
        "{Method}: {Uri} returned {Status} ({ContType} / {len} bytes) after {Elapsed}ms"
      );
  }

}