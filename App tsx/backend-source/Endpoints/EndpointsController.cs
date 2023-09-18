using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NuGet.Protocol;
using OperationCHAN.Areas.Lists.Pages;
using OperationCHAN.Controllers;
using OperationCHAN.Data;
using OperationCHAN.Hubs;
using OperationCHAN.Models;

namespace OperationCHAN.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class Ticket : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CourseController _courseController;
    private readonly IHubContext<HelplistHub> HubContext;
    private ILogger<Ticket> _logger;
    private readonly Helper _helper;

    public Ticket(ApplicationDbContext db, IHubContext<HelplistHub> hubContext, ILogger<Ticket> logger)
    {
        _db = db;
        _courseController = new CourseController(db);
        _logger = logger;
        HubContext = hubContext;
        _helper = new Helper(db, hubContext);
    }

    [HttpGet]
    public async Task<IActionResult> GetTicket(int id)
    {
        var ticket = _db.HelpList.FirstOrDefault(t => t.Id == id && t.Status == "WAITING");
        if (ticket == null)
        {
            return NotFound();
        }

        var helplist = _db.HelpList.Where(h => h.Course == ticket.Course && h.Status == "WAITING").ToList();

        var placement = _helper.GetTicketPosition(ticket.Id, helplist);
        
        var ticketModel = new EndpointsModels.TicketReturn()
        {
            Nickname = ticket.Nickname,
            Course = ticket.Course,
            Description = ticket.Description,
            Id = ticket.Id,
            Placement = placement,
            Room = ticket.Room
        };

        return Ok(ticketModel);
    }
    
    [HttpPost]
    public async Task<ActionResult<EndpointsModels.TicketReturn>> CreateTicket(EndpointsModels.Ticket ticketModel)
    {
        LongPollingController.PrintRequest(HttpContext, ticketModel.ToJson());
        
        var course = _courseController.RoomToCourse(ticketModel.Room);
        if (course == "")
        {
            return NotFound();
        }

        var helplistModel = _db.HelpList.FirstOrDefault(t => t.Nickname == ticketModel.Nickname
                                                             && t.Description == ticketModel.Description &&
                                                             t.Room == ticketModel.Room);
        if (helplistModel == null)
        {
            helplistModel = new HelplistModel
            {
                Nickname = ticketModel.Nickname,
                Room = ticketModel.Room,
                Course = course,
                Description = ticketModel.Description,
                Status = "WAITING"
            };
            
            _db.Add(helplistModel);
            _db.SaveChanges();
        }

        var helplist = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
            .Where(h=>h.Status.ToUpper() == "WAITING")
            .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room, h.Course}).ToList();

        _logger.LogInformation("Sending to {0}: {1}, {2}, {3}, {4}", course,
            helplistModel.Id, helplistModel.Nickname, helplistModel.Description, helplistModel.Room);
        await HubContext.Clients.Groups(course).SendAsync("AddToHelplist", 
            helplistModel.Id, helplistModel.Nickname, helplistModel.Description, helplistModel.Room, helplistModel.Course);
        

        var returnModel = new EndpointsModels.TicketReturn()
        {
            Nickname = ticketModel.Nickname,
            Room = ticketModel.Room.ToUpper(),
            Course = course,
            Description = ticketModel.Description,
            Id = helplistModel.Id,
            Placement = _helper.GetTicketPosition(helplistModel.Id, helplist)
        };

        return CreatedAtAction(nameof(CreateTicket), returnModel);
    }

    [HttpPut]
    public async Task<ActionResult> EditTicket(int id, EndpointsModels.Ticket ticketModel)
    {
        LongPollingController.PrintRequest(HttpContext, new Dictionary<string, string>()
        {
            {"id", id.ToString()},
            {"ticket", ticketModel.ToJson()}
        }.ToJson());

        var ticket = _db.HelpList.FirstOrDefault(t => t.Id == id);
        if (ticket == null)
        {
            _logger.LogInformation("Ticket not found");
            return NotFound();
        }
        var course = _courseController.RoomToCourse(ticketModel.Room);
        if (course == "")
        {
            _logger.LogInformation("Course not found");
            return NotFound();
        }

        var oldCourse = _courseController.RoomToCourse(ticket.Room);
        var newCourse = _courseController.RoomToCourse(ticketModel.Room);
        _logger.LogInformation("NewCourse: {0}, oldcourse: {1}", newCourse, oldCourse);
        var courseChanged = false;
        if (oldCourse != newCourse)
        {
            courseChanged = true;
            _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
                "RemoveFromHelplist", ticket.Id.ToString()
            });
            await HubContext.Clients.Groups(ticket.Course).SendAsync("RemoveFromHelplist", id);
        }
        
        ticket.Description = ticketModel.Description;
        ticket.Nickname = ticketModel.Nickname;
        ticket.Room = ticketModel.Room;
        ticket.Course = newCourse;
        
        await _db.SaveChangesAsync();

        _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
            "UpdateHelpList", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room, newCourse
        });
        await HubContext.Clients.Group(ticket.Course).SendAsync("UpdateHelplist", 
            ticket.Id, ticket.Nickname, ticket.Description, ticket.Room, newCourse);

        if (courseChanged)
        {
            var helplist = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
                .Where(h=>h.Status.ToUpper() == "WAITING")
                .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room, h.Course}).ToList();
            await _helper.SendToQueue(ticket.Id, newCourse,0, helplist);
        }

        var returnModel = new EndpointsModels.TicketReturn()
        {
            Nickname = ticket.Nickname,
            Room = ticket.Room.ToUpper(),
            Course = newCourse,
            Description = ticket.Description,
            Id = ticket.Id
        };

        return Ok(returnModel);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteTicket(EndpointsModels.TicketId ticketId)
    {
        var ticket = _db.HelpList.FirstOrDefault(t => t.Id == ticketId.Id);
        if (ticket == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
            "RemovedByUser", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room
        });
        await HubContext.Clients.All.SendAsync("RemovedByUser", ticket.Id);

        _db.Remove(ticket);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class Rooms : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public Rooms(ApplicationDbContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public ActionResult<List<string>> GetRooms()
    {
        LongPollingController.PrintRequest(HttpContext, "NONE");

        DateTime curTime = DateTime.Now;
        var courses = _db.Courses.Select(u => new
        {
            u.CourseCode, u.LabStart, u.LabEnd, u.CourseRoom1, u.CourseRoom2, u.CourseRoom3, u.CourseRoom4
        });
        
        var returnableRooms = new List<string>();
        foreach (var course in courses)
        {
            if ((course.LabStart < curTime) && (curTime < course.LabEnd))
            {
                if (course.CourseRoom1 != "")
                {
                    if (!returnableRooms.Contains(course.CourseRoom1))
                    {
                        returnableRooms.Add(course.CourseRoom1);
                    }
                }
                if (course.CourseRoom2 != "")
                {
                    if (!returnableRooms.Contains(course.CourseRoom2))
                    {
                        returnableRooms.Add(course.CourseRoom2);
                    }
                }
                if (course.CourseRoom3 != "")
                {
                    if (!returnableRooms.Contains(course.CourseRoom3))
                    {
                        returnableRooms.Add(course.CourseRoom3);
                    }
                }
                if (course.CourseRoom4 != "")
                {
                    if (!returnableRooms.Contains(course.CourseRoom4))
                    {
                        returnableRooms.Add(course.CourseRoom4);
                    }
                }
            }
        }
        return Ok(returnableRooms);
    }
}

[ApiController]
[Route("api/[controller]")]
public class Courses : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly RolesController _rolesController;

    public Courses(ApplicationDbContext db, UserManager<ApplicationUser> um)
    {
        _db = db;
        _rolesController = new RolesController(db, um);
    }


    [HttpGet, Route("all")]
    [Authorize]
    public ActionResult<List<string>> GetAllCourses()
    {
        LongPollingController.PrintRequest(HttpContext, "");

        var courses = _db.Courses.Select(c => c.CourseCode).Distinct().ToList();
        return Ok(courses);
    }
}

[ApiController]
[Route("api/[controller]")]
public class Helplist : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<HelplistHub> HubContext;
    private ILogger<Helplist> _logger;
    private Helper _helper;

    public Helplist(ApplicationDbContext db, IHubContext<HelplistHub> hubContext, ILogger<Helplist> logger)
    {
        _db = db;
        _logger = logger;
        HubContext = hubContext;
        _helper = new Helper(db, hubContext);
    }

    [HttpGet]
    [Authorize]
    public ActionResult<EndpointsModels.Helplist> GetHelplist(string course)
    {
        LongPollingController.PrintRequest(HttpContext, course);

        var courseModel = _db.Courses.FirstOrDefault(c => c.CourseCode == course);
        if (courseModel == null)
        {
            return NotFound();
        }

        var helplist = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
            .Where(h=>h.Status.ToUpper() == "WAITING")
            .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room}).ToList();

        return Ok(helplist);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> ChangeTicketStatus(int id)
    {
        LongPollingController.PrintRequest(HttpContext, id.ToString());

        var ticket = _db.HelpList.FirstOrDefault(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound();
        }
        
        var maxArchiveID = _db.HelpList.Where(t => t.Course == ticket.Course).Select(t => t.ArchiveID).Max();
        
        ticket.Status = "FINISHED";
        ticket.ArchiveID = maxArchiveID + 1;
        await _db.SaveChangesAsync();

        // Notify registered receivers of the update
        var course = ticket.Course;
        var helplist = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
            .Where(h=>h.Status.ToUpper() == "WAITING")
            .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room}).ToList();


        await HubContext.Clients.Groups(ticket.Course).SendAsync("RemoveFromHelplist", id);
        // Notify the helplist
        // _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
        //     "RemoveFromHelplist", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room
        // });
        // await HubContext.Clients.Groups(course).SendAsync("RemoveFromHelplist", id);
        
        // Notify the archive
        // _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
        //     "AddToArchive", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room
        // });

        await HubContext.Clients.Groups(course).SendAsync("AddToArchive", 
             ticket.Id, ticket.Nickname, ticket.Description, ticket.Status, ticket.Room);
        
        // Notify the queue
        await _helper.SendToQueue(id, ticket.Course, 0, helplist);

        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class Archive : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<HelplistHub> HubContext;
    private readonly ILogger<Archive> _logger;
    private readonly Helper _helper;

    public Archive(ApplicationDbContext db, IHubContext<HelplistHub> hubContext, ILogger<Archive> logger)
    {
        _db = db;
        HubContext = hubContext;
        _logger = logger;
        _helper = new Helper(db, hubContext);
    }

    [HttpGet]
    [Authorize]
    public ActionResult<IEnumerable<EndpointsModels.Helplist>> GetArchive(string course)
    {
        LongPollingController.PrintRequest(HttpContext, course);
        
        var courseModel = _db.Courses.FirstOrDefault(c => c.CourseCode == course);
        if (courseModel == null)
        {
            return NotFound();
        }

        var archive = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
            .Where(h => h.Status.ToUpper() == "FINISHED")
            .Select(h => new { h.Id, h.Nickname, h.Description, h.Room, h.ArchiveID })
            .OrderByDescending(h => h.ArchiveID).ToList();

        return Ok(archive);
    }
    
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> ChangeTicketStatus(int id)
    {
        LongPollingController.PrintRequest(HttpContext, id.ToString());

        // Get ticket
        var ticket = _db.HelpList.FirstOrDefault(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound();
        }

        ticket.Status = "WAITING";
        await _db.SaveChangesAsync();
        
        // Notify registered receivers of the update
        var course = ticket.Course;
        var helplist = _db.HelpList.Where(h => h.Course.ToUpper() == course.ToUpper())
            .Where(h=>h.Status.ToUpper() == "WAITING")
            .Select(h=> new {h.Id, h.Nickname, h.Description, h.Room}).ToList();


        await HubContext.Clients.Groups(ticket.Course).SendAsync("RemoveFromArchive", id);
        // Notify the archive
        // _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
        //     "RemoveFromArchive", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room
        // });
        // await HubContext.Clients.Groups(course).SendAsync("RemoveFromArchive", id);
        
        // Notify the helplist
        // _logger.LogInformation("Sending to course {0}: {1}", ticket.Course, new List<string>{
        //     "AddToHelplist", ticket.Id.ToString(), ticket.Nickname, ticket.Description, ticket.Room
        // });
        await HubContext.Clients.Groups(course).SendAsync("AddToHelplist", 
             ticket.Id, ticket.Nickname, ticket.Description, ticket.Room, ticket.Course);
        
        // Notify the queue
        await _helper.SendToQueue(id, ticket.Course, 1, helplist);
        
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class User : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly RolesController _rolesController;
    private readonly UserManager<ApplicationUser> _um;
    private readonly ForgottenPasswordController _forgottenPasswordController;

    public User(ApplicationDbContext db, UserManager<ApplicationUser> um, IEmailSender _emailSender, IUrlHelper URL)
    {
        _db = db;
        _um = um;
        _rolesController =  new RolesController(_db, _um);
        _forgottenPasswordController = new ForgottenPasswordController(_emailSender, _um, URL);
    }

    [HttpPut, Route("get")]
    [Authorize]
    public ActionResult<EndpointsModels.User> GetUser(EndpointsModels.EmailModel emailModel)
    {
        LongPollingController.PrintRequest(HttpContext, emailModel.ToJson());

        var userModel = _db.Users.FirstOrDefault(u => u.Email.ToLower() == emailModel.Email.ToLower());

        if (userModel == null)
        {
            return NotFound();
        }

        var role = "User";
        var roleObject = _db.UserRoles.FirstOrDefault(r => r.UserId == userModel.Id);
        if (roleObject != null)
        {
            role = _db.Roles.FirstOrDefault(r => r.Id == roleObject.RoleId).Name;
        }
        
        var newUserModel =
            new EndpointsModels.UserModel()
            {
                Id = userModel.Id,
                Nickname = userModel.Nickname,
                DiscordTag = userModel.DiscordTag,
                Email = userModel.Email.ToLower(),
                Role = role
            };

        return Ok(newUserModel);
    }

    [HttpPut, Route("validateDiscord")]
    public IActionResult CheckEmailExists(EndpointsModels.DiscordModel discordModel)
    {
        LongPollingController.PrintRequest(HttpContext, discordModel.ToJson());

        var userModel = _db.Users.FirstOrDefault(u => u.DiscordId == discordModel.DiscordId
        || u.Email == discordModel.Email);
        if (userModel == null)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet, Route("all")]
    [Authorize]
    public ActionResult<List<EndpointsModels.AllUsers>> GetAllUsers()
    {
        LongPollingController.PrintRequest(HttpContext, "NONE");

        var users = _db.Users.Select(u => new {u.Id, u.Nickname, u.Email, u.DiscordTag}).ToList();
        if (!users.Any())
        {
            return NotFound();
        }

        List<EndpointsModels.AllUsers> allUsers = new List<EndpointsModels.AllUsers>();

        foreach (var user in users)
        {
            var courses = _db.Studas
                .Where(c => c.ApplicationUserId == user.Id).Select(c => c.Course).ToList();
            var isAdmin = _rolesController.IsAdmin(user.Id);
            var newUser = new EndpointsModels.AllUsers()
            {
                ID = user.Id,
                Nickname = user.Nickname,
                DiscordTag = user.DiscordTag,
                Email = user.Email.ToLower(),
                IsAdmin = isAdmin,
                Courses = courses
            };
            allUsers.Add(newUser);
        }

        return Ok(allUsers);
    }
    
    [HttpPut, Route("courses")]
    [Authorize]
    public ActionResult<List<string>> GetCourses(EndpointsModels.EmailModel emailModel)
    {
        LongPollingController.PrintRequest(HttpContext, emailModel.ToJson());
        
        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == emailModel.Email.ToLower());
        if (user == null)
        {
            return NotFound();
        }

        var isAdmin = _rolesController.IsAdmin(user.Id);
        List<string> courses;
        if (isAdmin)
        {
            courses = _db.Courses.Select(c => c.CourseCode).OrderByDescending(c=>c).Distinct().ToList();
        }
        else
        {
            courses = _db.Studas.Where(c => c.ApplicationUserId == user.Id)
                .Select(c => c.Course).ToList();
        }
        if (!courses.Any())
        {
            return NotFound();
        }
        

        return Ok(courses);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult> EditUser(EndpointsModels.User userModel)
    {
        LongPollingController.PrintRequest(HttpContext, userModel.ToJson());

        var user = _db.Users.FirstOrDefault(u => u.Id == userModel.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.UserName = userModel.Nickname;
        user.Nickname = userModel.Nickname;
        user.Email = userModel.Email.ToLower();
        user.DiscordTag = userModel.DiscordTag;
        await _db.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpPut, Route("ChangePassword")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(EndpointsModels.ChangePassword userModel)
    {
        LongPollingController.PrintRequest(HttpContext, userModel.ToJson());

        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == userModel.Email.ToLower());
        if (user == null)
        {
            return Unauthorized();
        }

        var passCheck = await _um.CheckPasswordAsync(user, userModel.OldPassword);
        if (!passCheck)
        {
            return Unauthorized();
        }

        var tempUser = new ApplicationUser()
        {
            UserName = "PasswordValidator",
            Nickname = "PasswordValidator",
            Email = "password@validator.com"
        };
        var result = await _um.CreateAsync(
            tempUser,
            userModel.NewPassword
        );
        if (result.Succeeded)
        {
            _db.Remove(tempUser);

            await _um.ChangePasswordAsync(user, userModel.OldPassword, userModel.NewPassword);
            
            await _db.SaveChangesAsync();
            return NoContent();
        }
        foreach (var error in result.Errors) {
            ModelState.AddModelError(error.Code, error.Description);
        }
        
        return Unauthorized(ModelState);
    }
    
    [HttpPost, Route("ForgottenPassword")]
    
    public ActionResult ForgottenPassword(EndpointsModels.ForgottenPassword userModel)
    {
        LongPollingController.PrintRequest(HttpContext, userModel.ToJson());

        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == userModel.Email.ToLower());
        if (user == null)
        {
            return NoContent();
        }
        
        _forgottenPasswordController.HandleForgottenPassword(user);
        
        return NoContent();
    }

    [HttpPut, Route("ConfirmEmail")]
    public async Task<ActionResult<string> >ConfirmEmail(EndpointsModels.EmailModel emailModel)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == emailModel.Email.ToLower());
        if (user == null)
        {
            return NotFound();
        }

        user.EmailConfirmed = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<string>> DeleteUserData(EndpointsModels.UserID idModel)
    {
        LongPollingController.PrintRequest(HttpContext, idModel.ToJson());
        
        var user = _db.Users.FirstOrDefault(u => u.Id.ToUpper() == idModel.userID.ToUpper());
        if (user == null)
        {
            return NotFound();
        }

        var tickets = _db.HelpList.Where(t => t.Nickname == user.Nickname);

        _db.Users.Remove(user);
        _db.HelpList.RemoveRange(tickets);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class Roles : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly RolesController _rolesController;

    public Roles(ApplicationDbContext db, UserManager<ApplicationUser> um)
    {
        _db = db;
        _rolesController = new RolesController(db, um);
    }
    
    [HttpPut, Route("studass")]
    [Authorize]
    public async Task<ActionResult> SetStudass(EndpointsModels.StudassRole studass)
    {
        LongPollingController.PrintRequest(HttpContext, studass.ToJson());
        
        var userModel = _db.Users.FirstOrDefault(u => u.Id == studass.UserID);
        var courseModel = _db.Courses.FirstOrDefault(u => u.CourseCode == studass.Course);
        if (userModel == null)
        {
            return NotFound();
        }
        if (courseModel == null)
        {
            return NotFound();
        }

        await _rolesController.setStudass(userModel.Nickname, studass.Course, studass.Set);
        
        return NoContent();
    }
    
    [HttpPut, Route("admin")]
    [Authorize]
    public async Task<IActionResult> SetAdmin(EndpointsModels.AdminRole adminRole)
    {
        LongPollingController.PrintRequest(HttpContext, adminRole.ToJson());
        
        var usermodel = _db.Users.FirstOrDefault(u => u.Id == adminRole.UserID);
        
        if (usermodel == null)
        {
            return NotFound();
        }

        var response = _rolesController.SetAdmin(usermodel.Nickname, adminRole.Set).Result;
        //if (!success)
        //{
        //    ModelState.AddModelError("400", "Failed to set user to admin");
        //    return BadRequest(ModelState);
        //}
        
        return response;
    }
}

[ApiController]
[Route("api/[controller]")]
public class Timeedit : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public Timeedit(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public ActionResult<List<CourseLinksModel>> GetLinks()
    {
        LongPollingController.PrintRequest(HttpContext, "");

        var courseLinks = _db.CourseLinks.ToList();
        if (courseLinks.Count() == 0)
        {
            return NotFound();
        }
        
        return Ok(courseLinks);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EndpointsModels.TimeeditID>> AddLink(string link)
    {
        LongPollingController.PrintRequest(HttpContext, link);
        
        if(!link.StartsWith("https"))
        {
            ModelState.AddModelError("LinkNotSecured", "Link must be https");
            return BadRequest(ModelState);
        }
        if (!link.EndsWith(".html"))
        {
            ModelState.AddModelError("LinkNotValid", "Link must end with .html");
            return BadRequest(ModelState);
        }

        link = link.Replace("html", "ics");

        var courseLink = new CourseLinksModel(link);

        await _db.CourseLinks.AddAsync(courseLink);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(AddLink),new Dictionary<string, int>(){{"id",courseLink.Id}});
    }
    
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<string>> DeleteLink(int id)
    {
        LongPollingController.PrintRequest(HttpContext, id.ToString());

        var courseLink = _db.CourseLinks.FirstOrDefault(l => l.Id == id);
        if (courseLink == null)
        {
            return NotFound();
        }
        
        _db.CourseLinks.Remove(courseLink);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

[ApiController]
[Route(".well-known/acme-challenge/GuBiybYsUcn5RjKQ3FMd1R4Gqn4vuaqvGPnpgLM55u0")]
public class Certificate : ControllerBase
{
    [HttpGet]
    public String Cert()
    {
        return "GuBiybYsUcn5RjKQ3FMd1R4Gqn4vuaqvGPnpgLM55u0.TXs9rouWa_PFlpmRAFKD84oq8qAxHv_MIJnm4-scBnk";
    }
}
