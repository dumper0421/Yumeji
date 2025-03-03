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
    private TextMeshProUGUI lastPlayTime_;

    [SerializeField]
    private Image characterIcon_;

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
            sequenceNum_.text = data.SequenceNum.ToString();
            lastPlayTime_.text = data.LastPlayTime;
        }
    }

}
