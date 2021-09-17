using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    public partial class VolumeChange : Form
    {
        public VolumeChange()
        {
            InitializeComponent();
            AcceptButton = Enterbutton;
            clapnum.Text = ConfigurationManager.AppSettings["ClapVolume"];
            musicnum.Text = ConfigurationManager.AppSettings["MusicVolume"];
        }

        private void Enterbutton_Click(object sender, EventArgs e)
        {
            AddUpdateAppSettings("ClapVolume", clapnum.Text);
            AddUpdateAppSettings("MusicVolume", musicnum.Text);
            this.Close();
            this.Dispose();
        }

        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
