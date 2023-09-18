using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OperationCHAN.Controllers;
using OperationCHAN.Data;
using OperationCHAN.Models;

namespace OperationCHAN.Hubs;

public class SettingsHub : Hub
{
    private ApplicationDbContext _db;
    private RolesController _rolesController;
    public SettingsHub(ApplicationDbContext db, RolesController rolesController)
    {
        _db = db;
        _rolesController = rolesController;
    }
    
    /// <summary>
    /// Sets or removes the user as studass
    /// </summary>
    /// <param name="userName">The username of the user</param>
    /// <param name="courseCode">The course code to remove or add as studass to</param>
    /// <param name="setStudass">Booelan representing whether to set or remove as studass</param>
    public async Task SetStudass(string userName, string courseCode, bool setStudass)
    {

    }
    
    /// <summary>
    /// Set a user as admin
    /// </summary>
    /// <param name="userName">The username</param>
    /// <param name="setAdmin">Bool representing whether or not to set the user as admin</param>
    public async Task SetAdmin(string userName, bool setAdmin)
    {
        if (_rolesController.SetAdmin(userName, setAdmin).Result != new OkResult())
        {
            await SetError("You are the only admin, and thus cannot be removed. Set a new admin first");
        }

        _db.SaveChanges();
        // Update the client
        await GetUserData(userName);
    }
    
    /// <summary>
    /// Get user courses and admin status from database and send it to the client
    /// </summary>
    /// <param name="userName">The username to get data from</param>
    public async Task GetUserData(string userName)
    {
        bool isAdmin;
        // Get user data
        var user = _db.Users.First(user => user.Nickname == userName);
        var roleObject = _db.UserRoles.SingleOrDefault(userRole => userRole.UserId == user.Id);
        if (roleObject != null)
        {
            var roleID = roleObject.RoleId;
            var role = _db.Roles.First(role => role.Id == roleID).Name;
            // Check if the user is admin
            isAdmin = role.Equals("Admin");
        }
        else
        {
            isAdmin = false;
        }

        // Get the users courses
        var courses = _db.Studas.Where(studass => studass.ApplicationUserId == user.Id)
            .Select(studass => studass.Course).ToList();
        // Send the data to the client
        await Clients.Caller.SendAsync("ShowStudent", courses, isAdmin);
    }

    public async Task SetError(string error)
    {
        await Clients.Caller.SendAsync("ShowError", error);
    }
}