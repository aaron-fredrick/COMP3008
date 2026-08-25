using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace StudentBusinessTier
{
    [ServiceContract]
    public interface StudentBusinessInterface
    {
        [OperationContract]
        int GetNumEntries();
        [OperationContract]
        void GetValuesForEntry(int index, out string name, out int id, out string university);

        [OperationContract]
        void GetValuesForSearch(string searchText, out string name, out int id, out string university);

    }
}
