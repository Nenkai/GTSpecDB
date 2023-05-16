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
using System.Windows.Shapes;

using Humanizer;

using PDTools.SpecDB.Core;
namespace GTSpecDB.Editor
{
    /// <summary>
    /// Interaction logic for SpecDBKindSelector.xaml
    /// </summary>
    public partial class SpecDBKindSelector : Window
    {
        public SpecDBFolder SelectedType { get; set; }
        public bool HasSelected { get; set; }
        public SpecDBKindSelector()
        {
            InitializeComponent();

            foreach (var type in (SpecDBFolder[])Enum.GetValues(typeof(SpecDBFolder)))
                lb_Types.Items.Add(type.Humanize());

        }

        private void lb_Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btn_PickSpecDBType.IsEnabled = lb_Types.SelectedIndex != -1;
        }

        private void btn_PickSpecDBType_Click(object sender, RoutedEventArgs e)
        {
            if (lb_Types.SelectedIndex != -1)
            {
                SelectedType = (SpecDBFolder)lb_Types.SelectedIndex;
                HasSelected = true;
                Close();
            }
        }
    }
}
