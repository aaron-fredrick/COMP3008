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

using ConsoleApp1;

namespace WpfApp1
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string value = "";

            List<Student> studentlist = StudentList.Students();

            foreach (Student student in studentlist)
            {
                value = value + student.ToString()+"\n";
            }

            TextBox1.Text = value;
           
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                int StudentId = Int32.Parse(StudentID.Text);
                List<Student> studentlist = StudentList.Students();

                bool found = false;
                foreach (Student student in studentlist)
                {
                    if(student.Id == StudentId)
                    {
                        StudentID.Text = student.Id.ToString();
                        StudentName.Text = student.Name;
                        StudentUni.Text = student.University.ToString();
                        found = true;
                    }
                }
                if (!found)
                {
                    MessageBox.Show("Could not find, please try again");
                }

            }
            catch (Exception exception){
                MessageBox.Show("Please input valid id");
                MessageBox.Show(exception.Message);
            }
            
        }
    }
}
