using System.Windows.Forms;

namespace LeagueFPSBoost.Updater.MessageBoxCollection
{
    interface IUpdaterMessageBox
    {
        DialogResult ShowMessageBox();
        bool GetRequiresSpecialCall();
    }
}
