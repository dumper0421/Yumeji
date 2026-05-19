using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour
{
    
    public int SlotIndex;

    public bool isSave = true;

    #region SlotInfo
    [Header("SlotInfo")]

    [SerializeField]
    private GameObject slotInfo_;

    [SerializeField]
    private TextMeshProUGUI newGameText_;

    [SerializeField]
    private TextMeshProUGUI slotName_;

    [SerializeField]
    private TextMeshProUGUI sequenceNum_;

    [SerializeField]
    private TextMeshProUGUI playTime_;

    [SerializeField]
    private List<Image> characterIcons_;

    #endregion

    private void Awake()
    {
    }

    public void SetSaveSlot(PlayerSaveData data)
    {
        bool hasData = data != null;

        slotInfo_.SetActive(hasData);
        newGameText_.gameObject.SetActive(!hasData);

        if (hasData)
        {
            slotName_.text = SlotIndex.ToString();
            sequenceNum_.text = GameManager.Instance.GetSequenceNameByNum(data.SequenceNum);
            playTime_.text = data.PlayTime;
            characterIcons_[0].sprite = ResourceManager.Instance.Load<Sprite>("CharactersIcon/" + data.CharacterName+"Icon");
            
            if (data.CompanionName != null && data.CompanionName.Length > 0)
            {
                string[] companaionNames = data.CompanionName.Split(',');
                // 동료가 2명일 떄
                if (companaionNames.Length == 2)
                {
                    characterIcons_[2].sprite = ResourceManager.Instance.Load<Sprite>("CharactersIcon/" + companaionNames[1] + "Icon");
                    characterIcons_[2].gameObject.SetActive(true);
                }

                characterIcons_[1].sprite = ResourceManager.Instance.Load<Sprite>("CharactersIcon/" + companaionNames[0] + "Icon");
                characterIcons_[1].gameObject.SetActive(true);
            }
            else
            {
                characterIcons_[1].gameObject.SetActive(false);
                characterIcons_[2].gameObject.SetActive(false);
            }
        }
    }

}
