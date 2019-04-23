using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

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
                cb.Content = item.Title;
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
