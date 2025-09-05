using Godot;
using System;

namespace FastDragon
{
    public partial class SaveSlotManagementMenu : Page
    {
        [Export(PropertyHint.File, hintString: "*.tscn")] public string NewGameLevel;
        public void OnSlotChosen(int slotNumber)
        {
            if (SaveFileManager.Instance.SlotHasData(slotNumber))
            {
                SaveFileManager.Instance.LoadFromSlot(slotNumber);
            }
            else
            {
                SaveFileManager.Instance.StartNewGame(slotNumber, NewGameLevel);
            }
        }
    }
}
