using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/DialogueLoader")]
public class DialogueLoader : ScriptableObject
{
    public Dictionary<string, Dialogue> LoadDialogues(string DialogueName)
    {
        var asset = Resources.Load<TextAsset>($"Dialogues/{DialogueName}");
        if (asset == null) return null;

        var wrapper = JsonUtility.FromJson<DialogueArrayWrapper>(
            "{\"dialogues\":" + asset.text + "}"
        );
        var map = new Dictionary<string, Dialogue>();
        foreach (var dlg in wrapper.dialogues)
            map[dlg.id] = dlg;
        return map;
    }

    [System.Serializable]
    private class DialogueArrayWrapper { public Dialogue[] dialogues; }
}
