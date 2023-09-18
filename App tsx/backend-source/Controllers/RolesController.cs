using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperationCHAN.Data;
using OperationCHAN.Models;

namespace OperationCHAN.Controllers;

public class RolesController
{
    private ApplicationDbContext _db;
    private UserManager<ApplicationUser> _um;
    private RoleManager<IdentityRole> _rm;
    
    public RolesController(ApplicationDbContext db, UserManager<ApplicationUser> um)
    {
        _db = db;
        _um = um;
    }

    public string GetRole(string userID)
    {
        var roleObject = _db.UserRoles.FirstOrDefault(userRole => userRole.UserId == userID);
        if (roleObject != null)
        {
            var roleID = roleObject.RoleId;
            var role = _db.Roles.FirstOrDefault(role => role.Id == roleID).Name;
            return role;
        }

        return "User";
    }

    public bool IsAdmin(string userID)
    {
        var roleObject = _db.UserRoles.FirstOrDefault(userRole => userRole.UserId == userID);
        if (roleObject != null)
        {
            var roleID = roleObject.RoleId;
            var role = _db.Roles.First(role => role.Id == roleID).Name;
            if (role == "Admin")
            {
                return true;
            }
        }
        return false;
    }

    public bool IsStudass(string userID, string course)
    {
        var s = _db.Studas.FirstOrDefault(s =>
            s.ApplicationUserId == userID
            && s.Course == course);
        if (s == null)
        {
            return false;
        }

        return true;
    }
    
    public async Task<IActionResult> SetAdmin(string nickName, bool setAdmin)
    {
        // Get the user by nickname
        ApplicationUser user = _db.Users.FirstOrDefault(user => user.Nickname == nickName);
        if (user == null)
        {
            return new NotFoundResult();
        }
        
        var roleObject = _db.UserRoles.FirstOrDefault(userRole => userRole.UserId == user.Id);
        var role = "User";
        if (roleObject != null)
        {
            var roleID = roleObject.RoleId;
            role = _db.Roles.First(role => role.Id == roleID).Name;
        }
        
        if (setAdmin)
        {
            // If the current role is studass, remove all courses
            if (role == "Studass")
            {
                var courses = _db.Studas.Where(s => s.ApplicationUserId == user.Id).ToList();
                foreach (var course in courses)
                {
                    try
                    {
                        _db.Studas.Remove(course);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to remove iteratively: {0}", e);
                        //ModelState.AddModelError("400", "Failed to set user to admin");
                        return new OkResult();
                    }

                    await _db.SaveChangesAsync();
                }
            }
            
            // Set role
            await _um.AddToRoleAsync(user, "Admin");
            await _um.RemoveFromRoleAsync(user, "Studass");
        }
        else
        {
            // Check how many admins there are 
            var adminRoleId = _db.Roles.First(role => role.Name == "Admin").Id;
            var admins = _db.UserRoles.Where(role => role.RoleId == adminRoleId).ToList();
            if (admins.Count <= 1)
            {
                return new OkResult();
            }
            else
            {
                // Remove roles, and set them to user
                await _um.RemoveFromRoleAsync(user, "Admin");
            }
        }

        return new OkResult();
    }

    public async Task<bool> setStudass(string nickName, string courseCode, bool setStudass)
    {
        var user = _db.Users.FirstOrDefault(user => user.Nickname == nickName);
        var roleObject = _db.UserRoles.FirstOrDefault(userRole => userRole.UserId == user.Id);
        var role = "User";
        if (roleObject != null)
        {
            var roleID = roleObject.RoleId;
            role = _db.Roles.First(role => role.Id == roleID).Name;
        }

        // A user cant be both an admin and a studass, so return if already admin
        if (setStudass)
        {

            if (role == "Admin")
            {
                return true;
            }
            
            // Set as studass
            _um.AddToRoleAsync(user, "Studass");
            try
            {
                _db.Studas.Add(new Studas(user, courseCode));
            }
            catch (Exception e)
            {
                return false;
            }
        }
        else
        {
            // Get the studass data from the studass
            Studas studas = _db.Studas.FirstOrDefault(studas => studas.ApplicationUserId == user.Id 
                                                       && studas.Course == courseCode);
            if (studas == null)
            {
                return false;
            }
            
            // Remove the user as studass
            try
            {
                _db.Studas.Remove(studas);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Studass not removed: {0}", e);
            }
            var otherCourses = _db.Studas.Where(s => s.ApplicationUserId == user.Id
                                                     && s.Course != courseCode).ToList();
            
            // If the user is not studass in other courses, remove the studass role
            if (otherCourses.Count <= 0)
            {
                _um.RemoveFromRoleAsync(user, "Studass");
                try
                {
                    _db.Studas.Remove(new Studas(user, courseCode));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to remove studass: {0}", e);
                    return false;
                }
            }
        }
        await _db.SaveChangesAsync();
        return true;
    }
}