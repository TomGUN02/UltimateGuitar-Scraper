using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace UltimateGuitar_Scraper
{
    /// <summary>
    /// Interaction logic for TabWindow.xaml
    /// </summary>
    public partial class TabWindow : Window
    {
        public TabWindow(string tab)
        {
            InitializeComponent();
            TabText.Text = tab;
        }

        private void TabCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TabSaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "txt file (*.txt)|*.txt|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                bool? dialogResult = dialog.ShowDialog();
                switch (dialogResult)
                {
                    case true:
                        File.WriteAllText(dialog.FileName, TabText.Text);
                        break;
                    case false:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(exception.Message);
                throw;
            }

        }
    }
}
