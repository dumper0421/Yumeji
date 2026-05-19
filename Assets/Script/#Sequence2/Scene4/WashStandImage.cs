using System.Collections;
using UnityEngine;

public class WashStandImage : InspectableObject
{
    [Header("띄울 이미지 UI 패널")]
    [SerializeField] private GameObject imagePanel;

    [Header("플레이어 이동 스크립트")]
    [SerializeField] private PlayerMove_Test_Lerp playerMove;

    private bool isOpen = false;

    private void Start()
    {
        if (imagePanel != null)
            imagePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CloseImage();
        }
    }

    protected override void OnInspect()
    {
        if (isOpen)
            return;

        OpenImage();
    }

    private void OpenImage()
    {
        if (imagePanel == null)
        {
            Debug.LogWarning("Image Panel이 연결되지 않았습니다.");
            return;
        }

        imagePanel.SetActive(true);
        isOpen = true;

        if (playerMove != null)
            playerMove.canMove = false;
    }

    private void CloseImage()
    {
        imagePanel.SetActive(false);
        isOpen = false;

        StartCoroutine(EnableMoveNextFrame());
    }

    private IEnumerator EnableMoveNextFrame()
    {
        yield return null;

        if (playerMove != null)
            playerMove.canMove = true;
    }
}