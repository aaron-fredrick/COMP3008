using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using StudentBusinessTier;
using StudentDatabase;

namespace AsyncClient
{
    public delegate Student Search(string value); //delegate for searching
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StudentBusinessInterface foob;
        private string  searchvalue; 
       
        public MainWindow()
        {
                   InitializeComponent();
                   ChannelFactory<StudentBusinessInterface> foobFactory;
                   NetTcpBinding tcp = new NetTcpBinding();
                   //Set the URL and create the connection!
                   string URL = "net.tcp://localhost:8200/StudentBusinessService";
                   foobFactory = new ChannelFactory<StudentBusinessInterface>(tcp, URL);
                   foob = foobFactory.CreateChannel();
                   //Also, tell me how many entries are in the DB.
                   TotalNum.Text = foob.GetNumEntries().ToString();
           
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            string name = null;
            int id = 0;
            string universityName = null;
            foob.GetValuesForEntry(Int32.Parse(TotalNum.Text), out name, out id, out  universityName);
            SiD.Text = id.ToString();
            SName.Text = name;
            SUni.Text = universityName;
        }

        private async void SearchButthon_Click(object sender, RoutedEventArgs e)
        {
            searchvalue = SearchBox.Text;
            Task<Student> task = new Task<Student>(SearchDB);
            task.Start();
            statusLabel.Content = "Searching starts.....";
            Student student = await task;
            UpdateGui(student);
            statusLabel.Content = "Searching ends.....";
        }

       

        private Student SearchDB()
        {
            string name = null;
            int id = 0;
            string universityName = null;
            foob.GetValuesForSearch(searchvalue, out name, out id, out universityName);
            if (id != 0)
            {
                Student aStudent = new Student();
                aStudent.Name= name;
                aStudent.Id= id;
                aStudent.University = universityName;
                return aStudent;
         
            }
            return null;
        }

        private void UpdateGui(Student aStudent)
        {
            SiD.Text = aStudent.Id.ToString();
            SName.Text = aStudent.Name;
            SUni.Text = aStudent.University;
        }

    }
}
