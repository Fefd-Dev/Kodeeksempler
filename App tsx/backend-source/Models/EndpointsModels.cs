namespace OperationCHAN.Models;

public static class EndpointsModels
{
    public class Ticket
    {
        public string Nickname { get; set; } = String.Empty;
        public string Room { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
    }


    public class TicketId
    {
        public TicketId(int id)
        {
            Id = id;
        }

        public int Id { get; set; } = 0;
    }

    public class TicketReturn
    {
        public string Nickname { get; set; } = String.Empty!;
        public string Room { get; set; } = String.Empty!;
        public string Description { get; set; } = String.Empty;
        public string Course { get; set; } = String.Empty!;
        public int Id { get; set; } = 0;
        public int Placement { get; set; } = 0;
    }

    public class Helplist
    {
        public Helplist(int id, string nickname, string description, string room)
        {
            Id = id;
            Nickname = nickname;
            Description = description;
            Room = room;
        }

        public int Id { get; set; } = 0;
        public string Nickname { get; set; } = String.Empty;
        
        public string Description { get; set; } = String.Empty;
        
        public string Room { get; set; } = String.Empty;

    }

    public class ChangePassword
    {
        public ChangePassword(string email, string oldPassword, string newPassword)
        {
            Email = email;
            OldPassword = oldPassword;
            NewPassword = newPassword;
        }
        public string Email { get; set; } = String.Empty;
        
        public string OldPassword { get; set; } = String.Empty;
        public string NewPassword { get; set; } = String.Empty;
    }
    
    public class ForgottenPassword
    {
        public ForgottenPassword(string email)
        {
            Email = email;
        }
        public string Email { get; set; } = String.Empty;
    }

    public class EmailModel
    {
        public EmailModel(string email)
        {
            Email = email;
        }

        public string Email { get; set; } = String.Empty;
    }

    public class DiscordModel
    {
        public DiscordModel(string email, string discordId)
        {
            Email = email;
            DiscordId = discordId;
        }

        public string Email { get; set; } = string.Empty;
        public string DiscordId { get; set; } = string.Empty;
    }

    public class UserModel
    {
        public string Id { get; set; } = String.Empty;
        public string Nickname { get; set; } = String.Empty;
        public string DiscordTag { get; set; } = String.Empty;
        
        public string Email { get; set; } = String.Empty;
        
        public string Role { get; set; } = String.Empty;

    }

    public class User
    {
        public User(string nickname, string email, string discordTag)
        {
            Nickname = nickname;
            Email = email;
            DiscordTag = discordTag;
        }

        public string Id { get; set; } = String.Empty;
        public string Nickname { get; set; } = String.Empty;
        
        public string Email { get; set; } = String.Empty;
        public string DiscordTag { get; set; } = String.Empty;
    }

    public class StudassRole
    {
        public StudassRole(string userID, string course, bool set)
        {
            UserID = userID;
            Course = course;
            Set = set;
        }
        
        public string UserID { get; set; } = String.Empty;
        
        public string Course { get; set; } = String.Empty;

        public bool Set { get; set; } = false;
    }

    public class AdminRole
    {
        public AdminRole(string userID, bool set)
        {
            UserID = userID;
            Set = set;
        }
        
        public string UserID { get; set; } = String.Empty;
        public bool Set { get; set; } = false;
    }
    
    public class AllUsers
    {
        public string ID { get; set; } = String.Empty;
        public string Nickname { get; set; } = String.Empty;
        
        public string Email { get; set; } = String.Empty;

        public bool IsAdmin { get; set; } = false;

        public string DiscordTag { get; set; } = String.Empty;

        public List<string> Courses { get; set; } = new List<string>();
    }

    public class TicketStatus
    {
        public int id = 0;
        
        public string status { get; set; } = String.Empty;
        
    }

    public class TimeeditID
    {
        public TimeeditID(int id)
        {
            Id = id;
        }
        
        public int Id { get; set; } = 0;
    }

    public class UserID
    {
        public string userID { get; set; } = String.Empty;
    }

}