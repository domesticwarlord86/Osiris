using System;
using System.Windows.Forms;

namespace OsirisPlugin
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