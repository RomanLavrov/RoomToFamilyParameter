using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace RoomToFamily
{
    /// <summary>
    /// Interaction logic for LevelSelection.xaml
    /// </summary>
    public partial class LevelSelection : UserControl
    {
        public List<Autodesk.Revit.DB.Level> Levels = new List<Level>();
        private List<CheckBox> cbList = new List<CheckBox>();

        public LevelSelection()
        {
            InitializeComponent();
        }

        public void GetLevels(List<Autodesk.Revit.DB.Level> levels)
        {
            int index = 0;
            Levels.AddRange(levels);
            foreach (var item in Levels)
            {
                CheckBox cb = new CheckBox();
                cb.Content = item.Name;
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
                else Levels.RemoveAt(index);
            }
            var ParentWindow = Window.GetWindow(this);
            ParentWindow.DialogResult = true;
            ParentWindow.Close();
        }
    }
}
