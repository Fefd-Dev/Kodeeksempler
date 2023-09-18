using OperationCHAN.Data;

namespace OperationCHAN.Controllers;

public class CourseController
{
    private readonly ApplicationDbContext _db;

    public CourseController(ApplicationDbContext db)
    {
        _db = db;
    }

    public string RoomToCourse(string room)
    {
        var course = _db.Courses.FirstOrDefault(c => c.CourseRoom1 == room
                                            || c.CourseRoom2 == room
                                            || c.CourseRoom3 == room
                                            || c.CourseRoom4 == room);
        if (course == null)
        {
            return "";
        }

        return course.CourseCode;
    }

    public string CourseToRoom(string course)
    {
        return "";
    }
}