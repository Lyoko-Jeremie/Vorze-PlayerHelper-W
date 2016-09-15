using System.Windows.Forms;

namespace Vorze_PlayerHelper
{
    public interface Player
    {
        string Name { get; }
        void SetupInstructionLabel(Label lbl);
        void SetupTextboxIP(TextBox tb);
        void SetupTextboxPORT(TextBox tb);
        void playerInformationRetrieval(string playerIP, int playerPort, ref PlayerStatus playerStatus, System.Action<string, bool> logger, System.Action<string> processCSV);
    }
}
