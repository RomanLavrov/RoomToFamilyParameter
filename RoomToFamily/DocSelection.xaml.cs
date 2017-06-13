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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RoomToFamily
{
    /// <summary>
    /// Interaction logic for DocSelection.xaml
    /// </summary>
    public partial class DocSelection : UserControl
    {
        public List<Document> Docs { get; set; }
        private List<CheckBox> cbList = new List<CheckBox>();

        public DocSelection()
        {
            InitializeComponent();
        }

        public void GetDocList(List<Document> Documents)
        {
            int index = 0;
            Docs = Documents;
            foreach (var item in Documents)
            {
                CheckBox cb = new CheckBox();
                int position = item.PathName.LastIndexOf('\\');
                cb.Content = item.PathName.Substring(position + 1);
                cb.Margin = new Thickness(0, 20 * index, 0, 0);
                cbList.Add(cb);
                Grid.Children.Add(cb);
                index++;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            foreach (var item in cbList)
            {
                if (item.IsChecked == true)
                {
                    index++;
                }
                else Docs.RemoveAt(index);
            }
            // MessageBox.Show(Docs.Count.ToString());

            var parentWindow = Window.GetWindow(this);
            parentWindow.DialogResult = true;
            parentWindow?.Close();
        }
    }
}
