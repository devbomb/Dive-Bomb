using Godot;
using System;
using System.Linq;

namespace FastDragon
{
    public partial class SaveSlotManagementMenu : Page
    {
        [Export(PropertyHint.File, hintString: "*.tscn")] public string NewGameLevel;
        [Export(PropertyHint.File, hintString: "*.tscn")] public string NewGameHubWorld;

        private int? _slotTargettedForDeletion;

        public override void OnPageEntered()
        {
            Refresh();
        }

        private void Refresh()
        {
            // Create a view for each slot
            var slots = new SlotView[3];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new SlotView(i);

                slots[i].SlotChosen += OnSlotChosen;
                slots[i].DeletePressed += OnDeleteButtonPressed;
            }

            // Replace the existing slot views with the ones we just created
            var slotsContainer = GetNode<Control>("%Slots");

            while (slotsContainer.GetChildCount() > 0)
            {
                var child = slotsContainer.GetChild(0);
                slotsContainer.RemoveChild(child);
                child.QueueFree();
            }

            foreach (var slot in slots)
            {
                slotsContainer.AddChild(slot);
            }

            // Set up focus neighbors
            for (int i = 0; i < slots.Length; i++)
            {
                var next = (i + 1 < slots.Length)
                    ? slots[i + 1]
                    : slots[0];

                var prev = (i - 1 >= 0)
                    ? slots[i - 0]
                    : slots[slots.Length - 1];

                slots[i].WireFocusNeighbors(prev, next);
            }

            // Focus the first slot
            slots[0].ChooseButton.GrabFocus();
        }

        public void OnSlotChosen(int slotNumber)
        {
            if (SaveFileManager.Instance.SlotHasData(slotNumber))
            {
                SaveFileManager.Instance.LoadFromSlot(slotNumber);
            }
            else
            {
                SaveFileManager.Instance.StartNewGame(
                    slotNumber,
                    NewGameLevel,
                    NewGameHubWorld
                );
            }
        }

        public void OnDeleteButtonPressed(int slotNumber)
        {
            _slotTargettedForDeletion = slotNumber;
            GetNode<Popup>("%ConfirmDeletePrompt").PopupCentered();
        }

        public void OnDeleteConfirmed()
        {
            SaveFileManager.Instance.EraseSlot(_slotTargettedForDeletion.Value);
            Refresh();
        }

        private partial class SlotView : HBoxContainer
        {
            [Signal] public delegate void SlotChosenEventHandler(int slotNumber);
            [Signal] public delegate void DeletePressedEventHandler(int slotNumber);

            public readonly int SlotNumber;

            public readonly Button ChooseButton;
            public readonly Button DeleteButton;

            public SlotView(int slotNumber)
            {
                SlotNumber = slotNumber;

                ChooseButton = new Button
                {
                    Text = SlotText(slotNumber),
                    Flat = true,
                    Alignment = HorizontalAlignment.Left,
                };

                ChooseButton.Pressed += () => EmitSignal(SignalName.SlotChosen, slotNumber);
                AddChild(ChooseButton);

                DeleteButton = new Button
                {
                    Text = "Delete",
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand,
                    Visible = SaveFileManager.Instance.SlotHasData(slotNumber),
                    Disabled = !SaveFileManager.Instance.SlotHasData(slotNumber),
                };
                DeleteButton.Pressed += () => EmitSignal(SignalName.DeletePressed, slotNumber);
                AddChild(DeleteButton);
            }

            public void WireFocusNeighbors(SlotView prevSlot, SlotView nextSlot)
            {
                ChooseButton.FocusNext = nextSlot.ChooseButton.GetPath();
                ChooseButton.FocusPrevious = prevSlot.ChooseButton.GetPath();

                if (SaveFileManager.Instance.SlotHasData(SlotNumber))
                {
                    ChooseButton.FocusNeighborRight = DeleteButton.GetPath();

                    // Force the player to navigate back to the "choose" button
                    // before they can select a different slot.
                    DeleteButton.FocusNext = ChooseButton.GetPath();
                    DeleteButton.FocusPrevious = ChooseButton.GetPath();
                    DeleteButton.FocusNeighborLeft = ChooseButton.GetPath();
                    DeleteButton.FocusNeighborTop = ChooseButton.GetPath();
                    DeleteButton.FocusNeighborBottom = ChooseButton.GetPath();
                    DeleteButton.FocusNeighborRight = ChooseButton.GetPath();
                }
            }

            private static string SlotText(int slotNumber)
            {
                if (!SaveFileManager.Instance.SlotHasData(slotNumber))
                    return "New Game";

                // Peek at the save file to learn which level it was saved in
                var saveFile = SaveFileManager.Instance.PeekSlot(slotNumber);

                string levelScenePath = saveFile.CurrentLevel;
                string levelName = AtlasCache.Instance.GetEntry(levelScenePath).HumanReadableName;

                return levelName;
            }
        }
    }
}
