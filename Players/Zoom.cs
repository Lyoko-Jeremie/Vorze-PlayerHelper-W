using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Vorze_PlayerHelper.Libraries.Telnet;

namespace Vorze_PlayerHelper.Players
{
    public class Zoom : Player
    {
        public string Name
        {
            get
            {
                return "Zoom Player";
            }
        }

        private static TelnetConnection tc;

        public void SetupInstructionLabel(Label lbl)
        {
            lbl.Text = "Requires the \r\nZoom Player control API\r\nto be enabled";
        }
        public void SetupTextboxIP(TextBox tb)
        {
            tb.DataBindings.Clear();
            tb.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Vorze_PlayerHelper.Properties.Settings.Default, "ZplayerIP", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            tb.Text = global::Vorze_PlayerHelper.Properties.Settings.Default.ZplayerIP;
        }
        public void SetupTextboxPORT(TextBox tb)
        {
            tb.DataBindings.Clear();
            tb.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Vorze_PlayerHelper.Properties.Settings.Default, "ZplayerPORT", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            tb.Text = global::Vorze_PlayerHelper.Properties.Settings.Default.ZplayerPORT;
        }

        public void playerInformationRetrieval(ref PlayerStatus playerStatus, Action<string, bool> logger, Action<string> processCSV)
        {
            string playerIP = Properties.Settings.Default.ZplayerIP;
            int playerPort = int.Parse(Properties.Settings.Default.ZplayerPORT);
            while (playerStatus.vorzeIsEnabled)
            {
                try
                {
                    tc = new TelnetConnection(playerIP, playerPort);
                    char[] charSeparator = new char[] { ' ' };

                    // Disable live playback reporting
                    if (tc.IsConnected)
                    {
                        tc.Write("1100 0" + Environment.NewLine);
                    }
                    Thread.Sleep(1000);

                    while (tc.IsConnected)
                    {
                        try
                        {
                            // Check if playing
                            tc.Write("1000" + Environment.NewLine);
                            Thread.Sleep(10);

                            string playing = (tc.Read());

                            if (playing.StartsWith("1000 "))
                            {
                                string[] playingSplit = playing.Split(charSeparator);
                                playing = playingSplit[1].Trim();

                                if (playing != "3")
                                {
                                    // Prevent flooding logs if already paused
                                    if (!playerStatus.playerInactive)
                                    {
                                        logger("Zoom player isn't playing video file (paused / stopped)", true);
                                    }
                                    playerStatus.playerInactive = true;
                                }
                                else
                                {
                                    if (playerStatus.playerInactive)
                                    {
                                        playerStatus.playerWasInactive = true;
                                    }

                                    playerStatus.playerInactive = false;
                                }
                            }

                            // If player is active then retrieve time position
                            if (!playerStatus.playerInactive)
                            {
                                // Get CSV filename exists, run this every time to make sure the playback file hasn't changed
                                //logger("Retrieving filename for CSV lookup", false);

                                tc.Write("1800" + Environment.NewLine);
                                Thread.Sleep(10);

                                string filenameResult = (tc.Read());
                                try
                                {
                                    filenameResult = filenameResult + (tc.Read());
                                }
                                catch (Exception)
                                {
                                }

                                if (filenameResult.StartsWith("1800 "))
                                {
                                    string csvFileName = filenameResult.Replace("1800 ", string.Empty).Trim();
                                    //logger(string.Format("CSV lookup filename: {0}", csvFileName), false);

                                    if (File.Exists(csvFileName))
                                    {
                                        processCSV(csvFileName);
                                    }
                                }


                                tc.Write("1120" + Environment.NewLine);

                                string timecode = (tc.Read());

                                if (timecode.StartsWith("1120 "))
                                {
                                    try
                                    {
                                        string[] timeCodeSplit = timecode.Split(charSeparator);
                                        timecode = timeCodeSplit[1].Trim();
                                        int intTimeCode;
                                        bool successfullyParsed = int.TryParse(timecode, out intTimeCode);

                                        if (successfullyParsed)
                                        {
                                            if (intTimeCode > 100)
                                            {
                                                intTimeCode = int.Parse(timecode) / 100;
                                            }

                                            playerStatus.currentPlayPositionMovie = intTimeCode;

                                            //Debug.WriteLine("Zoom player timecode (ms): " + intTimeCode);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
                catch (Exception)
                {
                    //logger("No connection could be made to Zoom Player, make sure you enabled the control API in Zoom player and that the player is running", true);
                    if (!playerStatus.playerInactive)
                    {
                        playerStatus.playerInactive = true;
                    }
                    Thread.Sleep(2500);
                }
            }
        }
    }
}
