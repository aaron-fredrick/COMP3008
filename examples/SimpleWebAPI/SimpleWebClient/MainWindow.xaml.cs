using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SimpleWebClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            RestClient restClient = new RestClient("http://localhost:5076");
            RestRequest restRequest = new RestRequest("/student/detail/{id}", Method.Get);
            restRequest.AddUrlSegment("id", SIndex.Text);
            RestResponse restResponse = restClient.Execute(restRequest);
            // Console.WriteLine(restResponse.Content);
            Student student = JsonConvert.DeserializeObject<Student>(restResponse.Content);
            SID.Text = student.Id.ToString();
            SName.Text = student.Name;
            SAge.Text = student.Age.ToString();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            RestClient restClient = new RestClient("http://localhost:5076");
            RestRequest restRequest = new RestRequest("/student/detail", Method.Post);
            Student student = new Student();
            student.Age = Int32.Parse(SAge.Text);
            student.Name = SName.Text;
            student.Id = Int32.Parse(SID.Text);

            restRequest.RequestFormat = RestSharp.DataFormat.Json;
            restRequest.AddBody(student);

            RestResponse restResponse = restClient.Execute(restRequest);
            // Console.WriteLine(restResponse.Content);
            MessageBox.Show(restResponse.Content);
        }
    }
}
