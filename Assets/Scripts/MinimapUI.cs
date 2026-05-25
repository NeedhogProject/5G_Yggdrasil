// MinimapUI.cs
// 미니맵 카메라가 플레이어를 따라다니며 위치를 표시한다.

using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    // 인스펙터에서 연결
    public Camera minimapCamera;
    public RawImage minimapImage;
    public Transform player;

    // 카메라 높이 (맵 크기에 따라 조절)
    public float cameraHeight = 50f;

    private void Start()
    {
        if (minimapCamera == null)
        {
            return;
        }

        // 렌더 텍스처를 코드로 생성해서 연결
        RenderTexture minimapTexture = new RenderTexture(256, 256, 16);
        minimapCamera.targetTexture = minimapTexture;

        if (minimapImage == null == false)
        {
            minimapImage.texture = minimapTexture;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        if (minimapCamera == null)
        {
            return;
        }

        // 카메라가 플레이어 바로 위를 따라다님
        Vector3 cameraPosition = new Vector3(
            player.position.x,
            player.position.y + cameraHeight,
            player.position.z
        );
        minimapCamera.transform.position = cameraPosition;
    }

    // HUDManager에서 호출해서 미니맵을 켜고 끈다
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}