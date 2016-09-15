using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Vorze_PlayerHelper
{
    public partial class Form1 : Form
    {
        // UI DELEGATE
        private delegate void UniversalVoidDelegate();

        public Player thePlayer;
        private Dictionary<string, Player> players;

        public Form1()
        {
            InitializeComponent();

            // init players and dropdown
            InitPlayerSelect();

            // Start log monitoring and cleanup old log file
            if (File.Exists("debug.log"))
            {
                File.Delete("debug.log");
            }

            // Init Vorze hardware
            VorzeHelper.Init(this);
        }


        #region Validating
        private void tbZplayerIP_Validating(object sender, CancelEventArgs e)
        {
            saveSettings();
        }

        private void tbZplayerPort_Validating(object sender, CancelEventArgs e)
        {
            saveSettings();
        }

        private void tbVorzeDir_Validating(object sender, CancelEventArgs e)
        {
            saveSettings();
        }

        private void playerSelect_Validating(object sender, CancelEventArgs e)
        {
            string new_pk = ((KeyValuePair<string, Player>)playerSelect.SelectedItem).Key, pk = global::Vorze_PlayerHelper.Properties.Settings.Default.PlayerKey;

            if (new_pk != pk)
                global::Vorze_PlayerHelper.Properties.Settings.Default.PlayerKey = new_pk;

            saveSettings();
        }
        #endregion

        #region Events
        private void btnBrowseVorzeDir_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.VorzeRootDIR;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbVorzeDir.Text = folderBrowserDialog1.SelectedPath;
            }

            saveSettings();
        }

        private void cbMonitoringEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (cbMonitoringEnabled.Checked)
            {
                VorzeHelper.ToggleVorze(true);
                VorzeHelper.startPlayer();
            }
            else
            {
                VorzeHelper.ToggleVorze(false);
                VorzeHelper.stopPlayer();
            }
            saveSettings();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                VorzeHelper.stopPlayer();
                saveSettings();
            }
            catch (Exception)
            {
            }
        }
        private void cbMinimizeToTray_CheckedChanged(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState && Properties.Settings.Default.minimizeToTray)
            {
                notifyIcon1.Visible = true;
                Hide();
            }
        }

        private void playerSelect_SelectedValueChanged(object sender, EventArgs e)
        {
            InitPlayerControls();
        }
        #endregion

        #region Log, Helpers
        public void ListviewScrollToBottom(ListView lv)
        {
            try
            {
                ControlInvoke(lv, () => lv.Items[lv.Items.Count - 1].EnsureVisible());
            }
            catch (Exception)
            {
            }
        }
        public void Log(string s)
        {
            try
            {
                ControlInvoke(lvLog, () => lvLog.Items.Insert(0, string.Format("[ {0} ] {1}", DateTime.Now, s)));
            }
            catch (Exception) { }
        }

        public static void ControlInvoke(Control control, Action function)
        {
            if (control.IsDisposed || control.Disposing)
                return;

            if (control.InvokeRequired)
            {
                control.Invoke(new UniversalVoidDelegate(() => ControlInvoke(control, function)));
                return;
            }
            function();
        }

        private void InitPlayerSelect()
        {
            players = new Dictionary<string, Player>();
            List<Player> allPlayers = GetImplemenations();
            foreach(Player p in allPlayers)
            {
                players[p.Name] = p;
            }

            playerSelect.DataSource = new BindingSource(players, null);
            playerSelect.DisplayMember = "Key";
            playerSelect.ValueMember = "Value";

            string pk = global::Vorze_PlayerHelper.Properties.Settings.Default.PlayerKey;
            playerSelect.SelectedValue = players.Keys.Contains(pk) ? players[pk] : players[players.Keys.First()];

            InitPlayerControls();
        }

        private void InitPlayerControls()
        {
            thePlayer = ((KeyValuePair<string,Player>)playerSelect.SelectedItem).Value;

            thePlayer.SetupInstructionLabel(label3);
            thePlayer.SetupTextboxIP(tbZplayerIP);
            thePlayer.SetupTextboxPORT(tbZplayerPort);
        }

        private void saveSettings()
        {
            Properties.Settings.Default.Save();
        }
        private List<Player> GetImplemenations()
        {
            Type type = typeof(Player);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => p != type && type.IsAssignableFrom(p));

            List<Player> allTypes = new List<Player>();
            System.Reflection.ConstructorInfo ctor;
            
            foreach (Type t in types)
            {
                ctor = t.GetConstructor(new Type[] { });
                if (ctor != null)
                    allTypes.Add((Player)ctor.Invoke(new object[] { }));
            }
            return allTypes;
        }
        #endregion
    }
}