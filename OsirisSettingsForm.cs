using System;
using System.Windows.Forms;

namespace LlamaLibrary
{
    public partial class OsirisSettingsForm : Form
    {
        public OsirisSettingsForm()
        {
            InitializeComponent();
        }

        private void OsirisSettings_Load(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = OsirisSettings.Instance;
        }
    }
}