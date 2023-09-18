using Lib.AspNetCore.ServerSentEvents;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.Protocol;
using Org.BouncyCastle.Asn1.X509;
using Index = Microsoft.EntityFrameworkCore.Metadata.Internal.Index;

namespace OperationCHAN.Controllers;

public static class LongPollingController
{
    public static async Task RegisterNewListener(this HttpContext ctx, string outController, string key, string data)
    {
        try
        {
            ctx.Response.Headers.Add("Cache-Control", "no-transform");
            ctx.Response.Headers.Add("Content-Type", "text/event-stream");
            ctx.Response.Headers.Add("Connection", "keep-alive");
            ctx.Response.Headers.Add("X-Accel-Buffering", "no");

            AddHttpEntry(ctx, outController, key, data);

            //await ctx.Response.Body.FlushAsync();

        } catch (Exception e)
        {
            Console.WriteLine("Error encountered: {0}", e);
            await Resend(outController, key, data);
        }

        Thread.Sleep(Constants.Duration);
    }

    public static async Task Resend(string controller, string key, string data)
    {
        Thread.Sleep(4000);
        SSESendDataAsync(controller, key, data);
    }

    public static async Task NotifyRegisteredReceivers(string controller, string key, string value)
    {
        SSESendDataAsync(controller, key, value);
    }
    
    public static async Task SSESendDataAsync(string controller, string key, string data)
    {
        controller = controller.ToUpper();
        key = key.ToUpper();
        RemoveDisconnectedListeners();
        var entries = HttpEntries;
        var counter = 0;
        foreach (var entry in entries)
        {
            HttpContext ctx = entry.Value.Context;
            string _controller = entry.Value.OutController;
            string _key = entry.Value.Key;

            // Check if right controller, e.g Helplist or Archive or Queue
            Console.WriteLine("Matching {0} with controller {1} to controller {2}",
                ctx.Connection.Id, _controller, controller);
            if (_controller == controller)
            {
                // Check if right key, e.g ikt201-g
                Console.WriteLine("Matching {0} with key {1} to key {2}",
                    ctx.Connection.Id, _key, key);
                if (_key == key)
                {
                    try
                    {
                        counter++;
                        Console.WriteLine("Trying to send SSE to {0}", ctx.Connection.Id);
                        foreach (var line in data.Split('\n'))
                            ctx.Response.WriteAsync("data: " + line + "\n\n").Wait();
                        Console.WriteLine("Written data");
                        ctx.Response.WriteAsync("\n").Wait();
                        Console.WriteLine("Written end");
                        ctx.Response.Body.FlushAsync().Wait();
                        Console.WriteLine("Flushed pipe");
                        ctx.Response.CompleteAsync().Wait();
                        Console.WriteLine("Completed");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Everything OK, but failed to send message to {0}: {1}", entry.Key, e);
                    }
                }
            }
        }

        if (counter == 0)
        {
            Console.WriteLine("Found no registered listeners with controller {0} and key {1}", controller, key);
        }
    }

    private static void RemoveDisconnectedListeners()
    {
        try
        {
            var count = 0;
            foreach (var ctx in HttpEntries)
            {
                count++;
                if (!isConnected(ctx.Value.Context))
                {
                    RemoveHttpEntry(ctx.Key);
                    Console.WriteLine("Removed controller {}", ctx.Key);
                }
            }

        }
        catch (Exception e)
        {
            
        }
    }

    private static bool isConnected(HttpContext ctx)
    {
        try
        {
            var con = ctx.Connection.Id;
        }
        catch
        {
            return false;
        }

        return true;
    }

    public class Constants
    {
        public static int Duration = 50000;
        public static string Helplist = "HELPLIST";
        public static string Archive = "ARCHIVE";
        public static string Queue = "QUEUE";
        public static string Test = "TEST";
        
        public static string NotFound = "";
    }

    public class HTTPEntry
    {
        public HTTPEntry(string id, HttpContext _context, string outController, string key)
        {
            ID = id;
            Context = _context;
            OutController = outController;
            Key = key;
        }
        
        public string ID { get; set; } = String.Empty;
        public HttpContext Context { get; set; } = null;
        public string OutController { get; set; } = String.Empty;
        public string Key { get; set; } = String.Empty;
    }

    private static Dictionary<string, HTTPEntry> HttpEntries = new Dictionary<string, HTTPEntry>();

    public static Dictionary<string, HTTPEntry> GetEntries()
    {
        return HttpEntries;
    }

    public static void AddHttpEntry(HttpContext context, string controller, string key, string data)
    {
        controller = controller.ToUpper();
        key = key.ToUpper();
        string ID = context.Connection.Id;

        if (HttpEntries.ContainsKey(context.Connection.Id))
        {
            HTTPEntry oldEntry = HttpEntries[context.Connection.Id];
            if (oldEntry.OutController != controller || oldEntry.Key != key)
            {
                oldEntry.OutController = controller;
                oldEntry.Key = key;
                oldEntry.Context = context;
                Console.WriteLine("\nListener {0} reconnected with controller {1} and key {2}", ID, controller, key);
                SSESendDataAsync(controller, key, data);
            }
        }

        else
        {
            HTTPEntry newEntry = new HTTPEntry(ID, context, controller, key);
            HttpEntries[context.Connection.Id] = newEntry;
            // Initialize the entry
            Console.WriteLine("\nRegistering brand new listener for {0} with controller {1} and key {2}",
                ID, controller, key);
            Console.WriteLine("Listener not initialized, sending initialization data");
            SSESendDataAsync(controller, key, data);
        }
    }

    public static bool RemoveHttpEntry(string ID)
    {
        try
        {
            HttpEntries.Remove(ID);
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public static void PrintRequest(HttpContext ctx, string data)
    {
        var now = DateTime.Now;
        Console.WriteLine("\n{3} - Incoming request: Method: {0}, URL: {1}, data {2} \n",
            ctx.Request.Method, ctx.Request.Path, data, now);
    }

    public static void PrintResponse(HttpContext ctx, string data)
    {
        var now = DateTime.Now;
        Console.WriteLine("\n{3} - Sending response: URL: {0} Code: {1}", ctx.Request.Path, data, now);
    }
}