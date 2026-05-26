using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BalcaoLivre.Online.Windows;

public sealed class WhatsAppLocalConnectorServer
{
    private readonly HttpListener _listener = new();
    private CancellationTokenSource? _cts;

    public int Port { get; }

    public WhatsAppLocalConnectorServer(int port)
    {
        Port = port;
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    }

    public Task StartAsync(Func<WhatsAppConnectorRequest, Task<WhatsAppConnectorResponse>> handler)
    {
        if (_cts is not null)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _listener.Start();
        _ = Task.Run(() => LoopAsync(handler, _cts.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        try
        {
            _cts?.Cancel();
            _listener.Stop();
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (HttpListenerException)
        {
        }

        _cts = null;
        return Task.CompletedTask;
    }

    private async Task LoopAsync(Func<WhatsAppConnectorRequest, Task<WhatsAppConnectorResponse>> handler, CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception ex) when (ex is ObjectDisposedException or HttpListenerException or InvalidOperationException)
            {
                break;
            }

            _ = Task.Run(async () => await HandleAsync(context, handler), token);
        }
    }

    private static async Task HandleAsync(HttpListenerContext context, Func<WhatsAppConnectorRequest, Task<WhatsAppConnectorResponse>> handler)
    {
        AddCors(context.Response);
        if (context.Request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 204;
            context.Response.Close();
            return;
        }

        var pathOk = string.Equals(context.Request.Url?.AbsolutePath, "/whatsapp/message", StringComparison.OrdinalIgnoreCase);
        if (!context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) || !pathOk)
        {
            await WriteJsonAsync(context.Response, new WhatsAppConnectorResponse { Ok = false, Reply = "Rota nao encontrada." }, 404);
            return;
        }

        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<WhatsAppConnectorRequest>(json, MainWindowJson.Options) ?? new WhatsAppConnectorRequest();
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await WriteJsonAsync(context.Response, new WhatsAppConnectorResponse { Ok = false, Reply = "Mensagem vazia." }, 400);
                return;
            }

            var response = await handler(request);
            await WriteJsonAsync(context.Response, response, response.Ok ? 200 : 400);
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException)
        {
            await WriteJsonAsync(context.Response, new WhatsAppConnectorResponse { Ok = false, Reply = ex.Message }, 500);
        }
    }

    private static void AddCors(HttpListenerResponse response)
    {
        response.Headers["Access-Control-Allow-Origin"] = "https://web.whatsapp.com";
        response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object payload, int statusCode)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, MainWindowJson.Options);
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}

public sealed class WhatsAppConnectorRequest
{
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string ChatId { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class WhatsAppConnectorResponse
{
    public bool Ok { get; set; }
    public string Reply { get; set; } = "";
    public bool AutoReply { get; set; }
}

internal static class MainWindowJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}
