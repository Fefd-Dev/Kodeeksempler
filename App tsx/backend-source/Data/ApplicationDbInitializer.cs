using OperationCHAN.Models;
using Microsoft.AspNetCore.Identity;
using OperationCHAN.Data;

namespace OperationCHAN.Data
{
    public class ApplicationDbInitializer
    {
        public static void Initialize(ApplicationDbContext db, UserManager<ApplicationUser> um,
            RoleManager<IdentityRole> rm)
        {
                        
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();    
            db.SaveChanges();
            
            InitializeTestCourses(db);
            InitializeTestUsers(db);
            InitializeMainUsers(db, um, rm);
            InitializeTestCourseLinks(db);

            db.SaveChanges();
        }
        private static void InitializeTestCourses(ApplicationDbContext db)
        {
            var ikt201 = new CourseModel("IKT201-G", new DateTime(2023, 12, 24), DateTime.Now,
                new[]
                {
                    "GRM C2 036"
                });
            var ikt202 = new CourseModel("IKT202-G", new DateTime(2023, 12, 24), DateTime.Now,
                new[]
                {
                    "GRM C2 040"
                });

            db.Courses.Add(ikt201);
            db.Courses.Add(ikt202);
        }

        private static void InitializeTestUsers(ApplicationDbContext db)
        {
                        var A = new HelplistModel
            {
                Room = "GRM C2 036",
                Course = "IKT201-G",
                Nickname = "Charlotte",
                Description = "When is my flight?",
                Status = "WAITING"
                
            };
            db.Add(A);
            
            var B = new HelplistModel
            {
                Room = "GRM C2 036",
                Course = "IKT201-G",
                Nickname = "Sondre",
                Description = "How to program?",
                Status = "WAITING"
                
            };
            db.Add(B);
            
            var C = new HelplistModel
            {
                Room = "GRM C2 036",
                Nickname = "Nikolai",
                Course = "IKT201-G",
                Description = "How to know when the worlds ends?",
                Status = "WAITING"
            };
            db.Add(C);
            
            var D = new HelplistModel
            {
                Room = "GRM C2 040",
                Nickname = "Sivert",
                Course = "IKT202-G",
                Description = "How to ChatGPT?",
                Status = "WAITING"
            };
            db.Add(D);
            
            var E = new HelplistModel
            {
                Room = "GRM C2 036",
                Nickname = "Markus",
                Course = "IKT201-G",
                Description = "IT WORKS!?",
                Status = "WAITING"
            };
            db.Add(E);
            
            var F = new HelplistModel
            {
                Room = "GRM C2 036",
                Nickname = "Pepsi",
                Course = "IKT201-G",
                Description = "Voff voffoff voff (Er det mat snart)",
                Status = "WAITING"
            };
            db.Add(F);
            
            var G = new HelplistModel
            {
                Room = "GRM C2 036",
                Nickname = "Akira",
                Course = "IKT201-G",
                Description = "Voff! (Skal vi ut snart)",
                Status = "WAITING"
            };
            db.Add(G);
            
            var H = new HelplistModel
            {
                Room = "GRM C4 040",
                Nickname = "Per Arne",
                Course = "IKT202-G",
                Description = "Eg veit ikkje kor eg pekar!",
                Status = "WAITING"
            };
            db.Add(H);
        }

        private static void InitializeMainUsers(ApplicationDbContext db, UserManager<ApplicationUser> um, RoleManager<IdentityRole> rm)
        {
            var adminRole = new IdentityRole("Admin");
            rm.CreateAsync(adminRole).Wait();

            var admin = new ApplicationUser()
            {
                Nickname = "cool Admin",
                UserName = "admin@uia.no",
                Email = "admin@uia.no",
                DiscordTag = "Admin User",
                EmailConfirmed = true,
                //Helplist = IKThelplist
            };
            um.CreateAsync(admin, "Password1.").Wait();
            um.AddToRoleAsync(admin, "Admin").Wait();
            

            var user = new ApplicationUser()
            {
                Nickname = "cool user",
                Email = "user@uia.no",
                UserName = "user@uia.no",
                DiscordTag = "User User", 
                EmailConfirmed = true
                // Helplist = PhysicsHelplist
            };
            
            var studAssRole = new IdentityRole("Studass");
            rm.CreateAsync(studAssRole).Wait();
            
            var studass = new ApplicationUser()
            {
                Nickname = "cool studass",
                Email = "studass@uia.no",
                UserName = "studass@uia.no",
                DiscordTag = "Studass User", 
                EmailConfirmed = true
            };

            um.CreateAsync(user, "Password1.").Wait();
            um.CreateAsync(studass, "Password1.").Wait();
            um.AddToRoleAsync(studass, "Studass").Wait();
            
            db.Studas.Add(new Studas(studass, "IKT201-G"));
            db.Studas.Add(new Studas(studass, "IKT202-G"));
        }

        private static void InitializeTestCourseLinks(ApplicationDbContext db)
        {
            db.CourseLinks.Add(new CourseLinksModel("https://cloud.timeedit.net/uia/web/tp/ri167XQQ737Z50QvY1067gZ6y9Y10097QY.ics"));
        }
        
    }
}