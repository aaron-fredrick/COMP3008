namespace SimpleWebAPI.Models.Student
{
    public class StudentList
    {
        static List<Student> students = new List<Student>();
        public static List<Student> AllStudents()
        {
            return students;
        }
        public static void AddStudent(Student student)
        {
            students.Add (student);
        }
        public static Student GetStudent(int id)
        {
            foreach (Student student in students)
            {
                if (student.Id == id)
                {
                    return student;
                }
            }
            return null;
        }
        public static void Generate()
        {
            Student s1 = new Student();
            s1.Id = 1;
            s1.Name = "Sajib";
            s1.Age = 30;
            students.Add(s1);

            Student s2 = new Student();
            s2.Id = 2;
            s2.Name = "Mistry";
            s2.Age = 30;

            students.Add(s2);
        }
    }
}
