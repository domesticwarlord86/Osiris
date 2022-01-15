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

        private void propertyGrid1_Click(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}