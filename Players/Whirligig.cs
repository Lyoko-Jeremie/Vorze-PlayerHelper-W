using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Vorze_PlayerHelper.Players
{
    public class Whirligig : Player
    {
        public string Name
        {
            get
            {
                return "Whirligig";
            }
        }

        private char[] cs = new char[] { ' ' };

        public void SetupInstructionLabel(Label lbl)
        {
            lbl.Text = "Requires the \r\nWhirligig TimeCodeServer\r\nto be enabled";
        }
        public void SetupTextboxIP(TextBox tb)
        {
            tb.DataBindings.Clear();
            tb.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Vorze_PlayerHelper.Properties.Settings.Default, "WhirligigIP", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            tb.Text = global::Vorze_PlayerHelper.Properties.Settings.Default.WhirligigIP;
        }
        public void SetupTextboxPORT(TextBox tb)
        {
            tb.DataBindings.Clear();
            tb.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Vorze_PlayerHelper.Properties.Settings.Default, "WhirligigPORT", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            tb.Text = global::Vorze_PlayerHelper.Properties.Settings.Default.WhirligigPORT;
        }

        public void playerInformationRetrieval(ref PlayerStatus playerStatus, Action<string, bool> logger, Action<string> processCSV)
        {
            Socket MainSock = null;
            int i;
            string playerIP = Properties.Settings.Default.WhirligigIP;
            int playerPort = int.Parse(Properties.Settings.Default.WhirligigPORT);

            while (playerStatus.vorzeIsEnabled)
            {
                try
                {
                    MainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    MainSock.ExclusiveAddressUse = false;
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(playerIP), playerPort);
                    MainSock.Connect(endpoint);

                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    StringBuilder data = new StringBuilder();
                    
                    logger("listening ...", true);

                    while ((i = MainSock.Receive(bytes)) != 0 && playerStatus.vorzeIsEnabled)
                    {
                        try
                        {
                            string tmp = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            data.Append(tmp);
                            if (!tmp.Contains("\n"))
                            {
                                continue;
                            }
                            string[] inputs = data.ToString().Split('\n');
                            foreach (string input in inputs)
                            {
                                if (input.Length > 0)
                                    HandleInput(input, ref playerStatus, logger, processCSV);
                            }

                            data.Clear();
                        }
                        catch (Exception)
                        {
                            //logger(e.Message, true);
                        }
                    }

                }
                catch (Exception)
                {
                    if (!playerStatus.playerInactive)
                    {
                        playerStatus.playerInactive = true;
                    }
                    Thread.Sleep(2500);
                    //logger(e.Message, true);
                }
                // Shutdown and end connection
                if (MainSock != null)
                    MainSock.Close();
            }
        }

        private void HandleInput(string s, ref PlayerStatus playerStatus, Action<string, bool> logger, Action<string> processCSV)
        {
            string[] input = s.Split(cs, 2);
            if (input[0].Trim() == "C" && input.Length > 1)
            {
                // FILENAME
                string csvFileName = input[1].Replace("\"", string.Empty).Trim();
                if (File.Exists(csvFileName))
                {
                    processCSV(csvFileName);
                }
            }
            else if (input[0].Trim() == "S")
            {
                // PAUSE
                if (!playerStatus.playerInactive)
                {
                    logger("Player isn't playing video file (paused / stopped)", true);
                    playerStatus.playerInactive = true;
                }
            }
            else if (input[0].Trim() == "P" && input.Length > 1)
            {
                double time;
                if (double.TryParse(input[1], out time))
                {
                    // PLAY
                    if (playerStatus.playerInactive)
                    {
                        playerStatus.playerWasInactive = true;
                        playerStatus.playerInactive = false;
                    }

                    playerStatus.currentPlayPositionMovie = (int)(time * 10);
                }
            }
            else
            {
                //logger(s + s.Length.ToString(), true);
            }
        }
    }
}
