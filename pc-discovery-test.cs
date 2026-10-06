using AutoForge.Desktop;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
var ip=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork&&a.IsIPv6LinkLocal==false&&a.GetAddressBytes() is var b&&(b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31));
if(ip==null)throw new Exception("Test-Heimnetz fehlt auf dem Build-Rechner");
using var service=new DiscoveryService();service.Start();
using var client=new UdpClient(new IPEndPoint(ip,0));
var endpoint=new IPEndPoint(ip,47771);
await client.SendAsync(Encoding.UTF8.GetBytes("{\"service\":\"autoforge-discovery\",\"v\":1,\"nonce\":\"bad\"}"),endpoint);
try { await client.ReceiveAsync(new CancellationTokenSource(400).Token);throw new Exception("Ungueltige Anfrage wurde beantwortet"); } catch(OperationCanceledException) { }
var nonce="0123456789abcdef0123456789abcdef";
await client.SendAsync(Encoding.UTF8.GetBytes("{\"service\":\"autoforge-discovery\",\"v\":1,\"nonce\":\""+nonce+"\"}"),endpoint);
var response=await client.ReceiveAsync(new CancellationTokenSource(3000).Token);
var answer=JsonNode.Parse(Encoding.UTF8.GetString(response.Buffer))!.AsObject();
if(answer.Count!=5||answer["nonce"]?.ToString()!=nonce||answer["version"]?.ToString()!="0.6.3"||answer["service"]?.ToString()!="autoforge-discovery")throw new Exception("Discovery-Antwort stimmt nicht");
Console.WriteLine("UDP Discovery bestanden: ungueltige Anfrage ignoriert, passende Nonce beantwortet, keine Codes/Schluessel/Befehle ausgegeben.");

