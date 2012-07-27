using System;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewMapSettingsDialog
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public NewMapSettingsDialog()
        {
            DataContext = this;
            InitializeComponent();
            styleBox.ItemsSource = Enum.GetNames(typeof(IsoTools.IsometricStyle));
            styleBox.SelectedIndex = 1;

            widthBox.Text = "26";
            heightBox.Text = "45";
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

    public class NewMapValidationRule : ValidationRule
    {
        int _minSize = 10;
        int _maxSize = 256;

        public int MinSize
        {
            get { return _minSize; }
            set { _minSize = value; }
        }

        public int MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        /// <summary>
        /// Validation rule for map size.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int size;
            // Is an int
            if (!int.TryParse((string)value, out size))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range?
            if ((size < _minSize) || (size > _maxSize))
            {
                var msg = string.Format("Map dimensions must be between {0} and {1}.", _minSize, _maxSize);
                return new ValidationResult(false, msg);
            }

            // Number is valid
            return new ValidationResult(true, null);
        }
    }
}
