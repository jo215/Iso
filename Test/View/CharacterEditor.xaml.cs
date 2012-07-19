using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for CharacterEditor.xaml
    /// </summary>
    public partial class CharacterEditor : Window
    {
        private ViewModel _viewModel;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewModel"></param>
        public CharacterEditor(ViewModel viewModel)
        {
            WindowStyle = WindowStyle.ToolWindow;
            InitializeComponent();
            _viewModel = viewModel;
            
        }

        /// <summary>
        /// Hide (do not close) the if the X button is clicked.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (Visibility != Visibility.Visible) return;
            Visibility = Visibility.Hidden;
            e.Cancel = true;
        }

    }
}
