using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using StudentDatabase;


namespace DatabaseServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext= false)]

    internal class StudentServerImplmentation : StudentServerInterface
    {  
        int StudentServerInterface.GetNumEntries()
        {
            return StudentList.Students().Count;
        }

        void StudentServerInterface.GetValuesForEntry(int index, out string name, out int id, out string university)
        {
            
            List<Student> slist = StudentList.Students();
            name = slist[index-1].Name;
            id = slist[index-1].Id;
            university = slist[index-1].University;
        }
    }
}
