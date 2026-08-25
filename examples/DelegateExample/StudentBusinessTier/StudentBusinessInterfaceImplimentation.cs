using DatabaseServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StudentBusinessTier
{
    internal class StudentBusinessInterfaceImplimentation : StudentBusinessInterface
    {
        private StudentServerInterface foob;
        public StudentBusinessInterfaceImplimentation()
        {
          
            ChannelFactory<StudentServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();
            //Set the URL and create the connection!
            string URL = "net.tcp://localhost:8100/StudentService";
            foobFactory = new ChannelFactory<StudentServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();
           

        }

        public int GetNumEntries()
        {
            return foob.GetNumEntries();
        }

        public void GetValuesForEntry(int index, out string name, out int id, out string university)
        {
            foob.GetValuesForEntry(index, out name, out id, out university);
        }

        public void GetValuesForSearch(string searchText, out string name, out int id, out string university)
        {
            name = null;
            id = 0;
            university = null;
            int numEntry = foob.GetNumEntries();
            for(int i=1; i<=numEntry; i++)
            {
                string sName;
                int sId;
                string sUni;
                foob.GetValuesForEntry(i, out sName, out sId, out sUni);
                if (sName.ToLower().Contains(searchText.ToLower()))
                {
                    name = sName;
                    id = sId;
                    university = sUni;
                    break;
                }
            }
            Thread.Sleep(5000); //Forced sleep for two seconds
        }
    }
}
