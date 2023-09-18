using System.Collections;
using Microsoft.AspNetCore.SignalR;
using NuGet.Protocol;
using OperationCHAN.Data;
using OperationCHAN.Models;

namespace OperationCHAN.Hubs;

public class Helper
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IHubContext<HelplistHub> _helplistHub;

    public Helper(ApplicationDbContext db, IHubContext<HelplistHub> helplistHub)
    {
        _applicationDbContext = db;
        _helplistHub = helplistHub;
    }
    
    public async Task SendToQueue(int id, string course, int fromArchive, IEnumerable helplist)
    {
        var counter = 0;
        bool found = false;
        foreach (var ticket in helplist)
        {
            counter++;
            var ticketId = (int)ticket.GetType().GetProperty("Id").GetValue(ticket, null);
            if (ticketId == id)
            {
                found = true;
            }
            else
            {
                Console.WriteLine("Sending to ticket with id {0}: {1}", ticketId, new List<string>()
                {
                    id.ToString(), fromArchive.ToString(), counter.ToString(), ""
                }.ToJson());
                await _helplistHub.Clients.All.SendAsync("Queue", ticketId, fromArchive, counter, "");
            }
        }

        if (!found)
        {
            // Check if the ticket is another course
            var ticket = _applicationDbContext.HelpList.FirstOrDefault(t => t.Id == id);
            if (ticket != null)
            {
                if (ticket.Course != course)
                {
                    Console.WriteLine("Updating ticket placement with id {0}", id);
                    var newHelplist = _applicationDbContext.HelpList.Where(h => h.Course.ToUpper() == ticket.Course.ToUpper())
                        .Where(h=>h.Status.ToUpper() == "WAITING")
                        .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room, h.Course}).ToList();
                    await SendToQueue(id, course, 0, newHelplist);
                    return;
                }
                else
                {
                    Console.WriteLine("Removing ticket with id {0}: {1}", id, new List<string>()
                    {
                        id.ToString(), "0", ""
                    }.ToJson());
                    await _helplistHub.Clients.All.SendAsync("Queue", id, fromArchive, 0, "");
                }
            }
        }
        else
        {
            Console.WriteLine("Sending to ticket with id {0}: {1}", id, new List<string>()
            {
                id.ToString(), fromArchive.ToString(), counter.ToString(), ""
            }.ToJson());
            await _helplistHub.Clients.All.SendAsync("Queue", id, fromArchive, counter, "");
        }

        if (counter == 0)
        {
            Console.WriteLine("Helplist is empty");
        }
    }

    public async Task SendTest(int id, int number)
    {
        Console.WriteLine("Sending to all tickets {0}", id, new List<string>()
        {
            "0", number.ToString(), ""
        }.ToJson());
        await _helplistHub.Clients.All.SendAsync("Queue", id.ToString(), "0", number.ToString(), "");
    }
    
    public int GetTicketPosition(int id, IEnumerable helplist)
    {
        var counter = 0;
        foreach (var ticket in helplist)
        {
            counter++;
            var ticketId = (int)ticket.GetType().GetProperty("Id").GetValue(ticket, null);
            if (id == ticketId)
            {
                break;
            }
        }
        return counter;
    }
}