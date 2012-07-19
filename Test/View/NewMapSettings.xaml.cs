using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewMapSettingsDialog : Window
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public NewMapSettingsDialog()
        {
            DataContext = this;
            InitializeComponent();
            styleBox.ItemsSource = Enum.GetNames(typeof(ISOTools.IsometricStyle));
            styleBox.SelectedIndex = 1;

            widthBox.Text = "26";
            heightBox.Text = "45";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

    public class NewMapValidationRule : ValidationRule
    {
        int minSize = 10;
        int maxSize = 100;

        public int MinSize
        {
            get { return this.minSize; }
            set { this.minSize = value; }
        }

        public int MaxSize
        {
            get { return this.maxSize; }
            set { this.maxSize = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int size;

            // Is an int
            if (!int.TryParse((string)value, out size))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range?
            if ((size < this.minSize) || (size > this.maxSize))
            {
                string msg = string.Format("Margin must be between {0} and {1}.", this.minSize, this.maxSize);
                return new ValidationResult(false, msg);
            }

            // Number is valid
            return new ValidationResult(true, null);
        }
    }
}
