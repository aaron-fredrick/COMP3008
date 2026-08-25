using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<Student> studentlist = StudentList.Students();
           
            foreach (Student student in studentlist)
            {
                Console.WriteLine("*******************************************************");
                Console.WriteLine(student);
            }
            Console.WriteLine("***********************************************************");
            Console.ReadLine();
        }
    }
}
