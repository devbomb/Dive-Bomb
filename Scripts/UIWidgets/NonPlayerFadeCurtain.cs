using Godot;

namespace FastDragon
{
    /// <summary>
    /// A fade curtain that makes the entire scene fade to black,
    /// _except the player_.  This is used for the transition to the "exit level" loading screen.
    /// </summary>
    public partial class NonPlayerFadeCurtain : Control
    {
        public float FadePercent = 0;

        private Control _viewportContainer;
        private Node _viewportContainerParent;
        private MeshInstance3D _blackBackground;

        public override void _Ready()
        {
            _viewportContainer = GetNode<Control>("%SubViewportContainer");
            _blackBackground = GetNode<MeshInstance3D>("%BlackBackground");
            _viewportContainerParent = _viewportContainer.GetParent();
        }

        public override void _Process(double delta)
        {
            // Detatch the viewport while invisible, to avoid unnecessary
            // rendering
            if (!Visible && _viewportContainer.GetParent() != null)
            {
                _viewportContainerParent.RemoveChild(_viewportContainer);
            }
            if (Visible && _viewportContainer.GetParent() == null)
            {
                _viewportContainerParent.AddChild(_viewportContainer);
            }

            // Adjust the transparency
            _blackBackground.Transparency = 1f - FadePercent;
        }
    }
}