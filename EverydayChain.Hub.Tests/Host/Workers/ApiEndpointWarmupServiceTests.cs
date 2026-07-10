using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Startup;
using EverydayChain.Hub.Host.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Host.Workers;

public sealed class ApiEndpointWarmupServiceTests
{
    [Fact]
    public async Task WarmupAsync_ShouldWarmAllConfiguredEndpoints()
    {
        var handler = new RecordingWarmupHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5188/")
        };
        var service = new ApiEndpointWarmupService(
            httpClient,
            Options.Create(new WebEndpointOptions
            {
                Url = "http://localhost:5188"
            }),
            NullLogger<ApiEndpointWarmupService>.Instance);

        await service.WarmupAsync(CancellationToken.None);

        Assert.Equal(45, handler.Requests.Count);
        Assert.All(
            handler.Requests,
            request => Assert.True(request.IsInternalWarmupRequest));
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Get && request.Path == "/health/live");
        Assert.Contains(handler.Requests, request => request.Path == "/api/v1/dashboard/export/xlsx");
        Assert.Contains(handler.Requests, request => request.Path == "/api/v1/box-tracking/export/xlsx");
        Assert.Contains(handler.Requests, request => request.Path == "/api/v1/drop-feedback/confirm");
        Assert.Contains(
            handler.Requests,
            request => request.Path == "/api/v1/scan/upload"
                && request.Body.Contains("\"barcodes\":[]", StringComparison.Ordinal));
        Assert.Contains(
            handler.Requests,
            request => request.Path == "/api/v1/dashboard/sync"
                && string.Equals(request.Body, "{", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Path == "/api/v1/wave-cleanup/execute");
    }

    private sealed class RecordingWarmupHandler : HttpMessageHandler
    {
        /// <summary>
        /// 获取已记录的预热请求集合。
        /// </summary>
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                body,
                string.Equals(
                    request.Headers.TryGetValues(InternalWarmupRequestMarker.HeaderName, out var values)
                        ? values.SingleOrDefault()
                        : null,
                    InternalWarmupRequestMarker.HeaderValue,
                    StringComparison.OrdinalIgnoreCase)));

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            return path switch
            {
                "/" => BuildTextResponse(HttpStatusCode.OK, "text/html", "<html>ok</html>"),
                "/health/live" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true}"),
                "/health/ready" => BuildJsonResponse(HttpStatusCode.ServiceUnavailable, "{\"data\":{\"apiWarmup\":{\"isCompleted\":false}}}"),
                "/api/v1/waves/current" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":{\"waveCode\":\"W1\",\"barcode\":\"B1\"}}"),
                "/api/v1/waves/options" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":{\"waveOptions\":[{\"waveCode\":\"W1\"}]}}"),
                "/api/v1/waves/list" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":{\"items\":[{\"waveId\":\"W1\"}]}}"),
                "/api/v1/business-query/tasks" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":{\"items\":[{\"taskCode\":\"T1\",\"barcode\":\"B1\",\"waveCode\":\"W1\",\"orderId\":\"O1\"}]}}"),
                "/api/v1/business-task-seed/manual" => BuildJsonResponse(HttpStatusCode.BadRequest, "{\"isSuccess\":false,\"message\":\"seed validation failed\"}"),
                "/api/v1/scan/upload" => BuildJsonResponse(HttpStatusCode.BadRequest, "{\"isSuccess\":false,\"message\":\"barcode required\"}"),
                "/api/v1/dashboard/sync" => BuildJsonResponse(HttpStatusCode.BadRequest, "{\"isSuccess\":false,\"message\":\"invalid json\"}"),
                "/api/v1/wave-cleanup/execute" => BuildJsonResponse(HttpStatusCode.BadRequest, "{\"isSuccess\":false,\"message\":\"wave code required\"}"),
                "/api/v1/drop-feedback/confirm" => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"message\":\"task not found\",\"data\":{\"isAccepted\":false}}"),
                _ when path.EndsWith("/export/xlsx", StringComparison.OrdinalIgnoreCase)
                    => BuildBinaryResponse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                _ when path.EndsWith("/export/csv", StringComparison.OrdinalIgnoreCase)
                    => BuildTextResponse(HttpStatusCode.OK, "text/csv", "ok"),
                _ => BuildJsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true,\"message\":\"ok\"}")
            };
        }

        private static HttpResponseMessage BuildJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static HttpResponseMessage BuildTextResponse(HttpStatusCode statusCode, string mediaType, string text)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(text, Encoding.UTF8, mediaType)
            };
        }

        private static HttpResponseMessage BuildBinaryResponse(string mediaType)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3, 4])
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return response;
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        string Path,
        string Body,
        bool IsInternalWarmupRequest);
}
