var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpClient("ChemyApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5192");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapStaticAssets();
app.UseRouting();
app.UseAuthorization();

// Transparent API Proxy: Forwards all frontend /api/v1 requests seamlessly to Chemy.Api microservice
app.Map("/api/v1/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ChemyApi");
    var targetUri = new Uri(client.BaseAddress!, context.Request.Path + context.Request.QueryString);

    using var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

    if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
    {
        requestMessage.Content = new StreamContent(context.Request.Body);
        if (context.Request.ContentType != null)
        {
            requestMessage.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    try
    {
        using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        context.Response.ContentType = responseMessage.Content.Headers.ContentType?.ToString() ?? "application/json";

        await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { error = "Chemy.Api microservice is unreachable at http://localhost:5192", details = ex.Message });
    }
});

app.MapGet("/healthz", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ChemyApi");
    try
    {
        using var response = await client.GetAsync("/healthz");
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = "application/json";
        await response.Content.CopyToAsync(context.Response.Body);
    }
    catch
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { status = "Degraded", message = "Chemy.Api offline, server-side fallback active" });
    }
});

app.MapRazorPages();

app.Run();
