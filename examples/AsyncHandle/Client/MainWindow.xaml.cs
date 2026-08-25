using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StudentBusinessInterface foob;
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
            foob.GetValuesForEntry(Int32.Parse(TotalNum.Text), out name, out id, out universityName);
            SiD.Text = id.ToString();
            SName.Text = name;
            SUni.Text = universityName;
        }

        private void SearchButthon_Click(object sender, RoutedEventArgs e)
        {
            string name = null;
            int id = 0;
            string universityName = null;
            foob.GetValuesForSearch(SearchBox.Text, out name, out id, out universityName);
            SiD.Text = id.ToString();
            SName.Text = name;
            SUni.Text = universityName;
        }
    }
}
