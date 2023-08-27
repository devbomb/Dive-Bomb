#if TOOLS
using System;
using System.IO;
using System.Diagnostics;
using Godot;

[Tool]
public partial class GenerateTrenchbroomEntityModels : EditorPlugin
{
    private const string MenuItemName = "Generate Trenchbroom Entity Models";
    public override void _EnterTree()
    {
        AddToolMenuItem(MenuItemName, new Callable(this, nameof(Generate)));
    }

    public override void _ExitTree()
    {
        RemoveToolMenuItem(MenuItemName);
    }

    private void Generate()
    {
        string projectFolder = Directory.GetCurrentDirectory();
        string blendFilesFolder = Path.Combine(projectFolder, "BlenderModels");
        string objFilesFolder = Path.Combine(projectFolder, "TrenchbroomEntityModels");

        foreach (var file in new DirectoryInfo(blendFilesFolder).EnumerateFiles())
        {
            if (file.Extension != ".blend")
                continue;

            string objFileName = $"{Path.GetFileNameWithoutExtension(file.Name)}.obj";
            string objFilePath = Path.Combine(objFilesFolder, objFileName);
            ExportToObj(file.FullName, objFilePath);
        }

    }

    private void ExportToObj(string blendFilePath, string outputFilePath)
    {
        string exportScriptPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "addons",
            "GenerateTrenchbroomEntityModels",
            "ExportObj.py"
        );

        string blenderFolder = GetEditorInterface()
            .GetEditorSettings()
            .GetSetting("filesystem/import/blender/blender3_path")
            .AsString();

        string blenderPath = Path.Combine(blenderFolder, "blender.exe");

        var process = Process.Start(
            blenderPath,
            new[]
            {
                blendFilePath,
                "--background",
                "--python",
                exportScriptPath,
                "--",
                outputFilePath
            }
        );
        process.WaitForExit();
    }
}
#endif
