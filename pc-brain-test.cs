using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using AutoForge.Desktop;
using var listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:8787/"); listener.Start();
var server = Task.Run(async () => {
    for (var i = 0; i < 3; i++) {
        var c = await listener.GetContextAsync(); JsonObject response;
        if(c.Request.Url!.AbsolutePath == "/api/status") response = new() { ["service"] = "autoforge-local-brain", ["root"] = RemoteBridgeService.ProjectRoot, ["version"] = "0.3.0" };
        else if(c.Request.Url.AbsolutePath == "/api/state") response = new() { ["token"] = "test-local-token" };
        else { if(c.Request.Headers["X-AutoForge-Token"] != "test-local-token") throw new Exception("Local token missing"); using var reader = new StreamReader(c.Request.InputStream); var p = JsonNode.Parse(await reader.ReadToEndAsync())!; if(p["requestId"]!.GetValue<string>() != "desktop-test" || p["chatId"]!.GetValue<string>() != "my-chat" || p["message"]!.GetValue<string>() != "Hallo" || p["command"] is not null) throw new Exception("Unsafe or missing chat payload"); response = new() { ["chatId"] = "my-chat", ["model"] = "test-local-model", ["message"] = new JsonObject { ["content"] = "Guten Tag" } }; }
        var bytes = Encoding.UTF8.GetBytes(response.ToJsonString()); c.Response.ContentType = "application/json"; await c.Response.OutputStream.WriteAsync(bytes); c.Response.Close();
    }
});
var brain = new LocalBrainBridgeService(); var r = await brain.ExecuteAsync("desktop-test", new JsonObject { ["operation"] = "chat", ["chatId"] = "my-chat", ["message"] = "Hallo", ["command"] = "must-not-pass" });
if(r["message"]?["content"]?.GetValue<string>() != "Guten Tag") throw new Exception("Chat result lost");
try { await brain.ExecuteAsync("invalid", new JsonObject { ["operation"] = "shell" }); throw new Exception("Arbitrary operation accepted"); } catch(InvalidOperationException) {}
await server; Console.WriteLine("Local chat bridge: authenticated request, preserved chat, model result and blocked arbitrary operation verified.");
namespace AutoForge.Desktop { public class RemoteBridgeService { public const string ProjectRoot = @"G:\gpt\AutoForge"; public Task<JsonObject> SearchWebAsync(string q) => Task.FromResult(new JsonObject()); } }

