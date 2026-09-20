using Godot;

namespace FastDragon
{
    public partial class SaveKeyGenerator : Node
    {
        public string SaveKey { get; private set; }

        public override void _Ready()
        {
            SaveKey = GenerateSaveKey();
        }

        private string GenerateSaveKey()
        {
            var builder = new System.Text.StringBuilder();
            Visit(GetParent());
            return builder.ToString();

            void Visit(Node n)
            {
                if (n.GetParent() == GetTree().Root)
                {
                    builder.Append(n.Name);
                    return;
                }

                Visit(n.GetParent());
                builder.Append("/");
                builder.Append(n.GetIndex());
            }
        }
    }
}