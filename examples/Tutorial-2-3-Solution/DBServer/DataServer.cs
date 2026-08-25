// Alex Starling - Distributed Computing - 2021
using System;
using System.Drawing;
using System.ServiceModel;
using DBInterface;
using DBLib;

namespace DBServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal class DataServer : DataServerInterface
    {
        private readonly Database _db = Database.Instance;

        public int GetNumEntries()
        {
            return _db.GetNumRecords();
        }

        public void GetValuesForEntry(int index, out uint acctNo, out uint pin, out int bal, out string fName, out string lName, out Bitmap icon)
        {
            // Check if index is out-of-range, if it is return an error
            if (index < 0 || index >= _db.GetNumRecords())
            {
                Console.WriteLine("Client tried to get a record that was out of range...");
                throw new FaultException<IndexOutOfRangeFault>(new IndexOutOfRangeFault()
                    {Issue = "Index was not in range..."});
            }
            acctNo = _db.GetAcctNoByIndex(index);
            pin = _db.GetPINByIndex(index);
            bal = _db.GetBalanceByIndex(index);
            fName = _db.GetFirstNameByIndex(index);
            lName = _db.GetLastNameByIndex(index);
            icon = new Bitmap(_db.GetIconByIndex(index));
        }
    }
}
