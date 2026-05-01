using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    [Header("Minimap Settings")]
    public Camera minimapCamera;
    public RawImage minimapImage;
    public Transform player;
    
    [Header("Minimap Properties")]
    public float zoomLevel = 20f;
    public float minZoom = 10f;
    public float maxZoom = 50f;
    public float zoomSpeed = 5f;
    public float cameraHeight = 50f;
    
    [Header("Icons")]
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;
    public GameObject npcIconPrefab;
    public GameObject itemIconPrefab;
    
    [Header("UI Elements")]
    public GameObject minimapPanel;
    public Button toggleButton;
    public Slider zoomSlider;
    
    private RenderTexture minimapTexture;
    private bool isMinimapActive = true;
    private List<GameObject> minimapIcons = new List<GameObject>();
    
    void Start()
    {
        SetupMinimap();
        SetupUI();
    }
    
    void Update()
    {
        if (isMinimapActive)
        {
            UpdateMinimapPosition();
            HandleZoom();
        }
    }
    
    void OnDestroy()
    {
        if (minimapTexture != null)
        {
            minimapTexture.Release();
        }
    }
    
    private void SetupMinimap()
    {
        // 미니맵 카메라 설정
        if (minimapCamera == null)
        {
            GameObject minimapCamObj = new GameObject("MinimapCamera");
            minimapCamera = minimapCamObj.AddComponent<Camera>();
        }
        
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = zoomLevel;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Player", "Enemy", "NPC");
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        minimapCamera.depth = 10; // 메인 카메라보다 높은 depth
        
        // 렌더 텍스처 생성
        minimapTexture = new RenderTexture(512, 512, 16);
        minimapCamera.targetTexture = minimapTexture;
        
        if (minimapImage != null)
        {
            minimapImage.texture = minimapTexture;
        }
    }
    
    private void SetupUI()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMinimap);
        }
        
        if (zoomSlider != null)
        {
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;
            zoomSlider.value = zoomLevel;
            zoomSlider.onValueChanged.AddListener(OnZoomSliderChanged);
        }
    }
    
    private void UpdateMinimapPosition()
    {
        if (player != null && minimapCamera != null)
        {
            Vector3 newPosition = player.position;
            newPosition.y = player.position.y + cameraHeight;
            minimapCamera.transform.position = newPosition;
        }
    }
    
    private void HandleZoom()
    {
        // 마우스 휠로 줌 조절
        float scrollInput = Mouse.current.scroll.ReadValue().y * 0.1f;
        
        if (scrollInput != 0f)
        {
            zoomLevel -= scrollInput * zoomSpeed;
            zoomLevel = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
            
            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = zoomLevel;
            }
            
            if (zoomSlider != null)
            {
                zoomSlider.value = zoomLevel;
            }
        }
    }
    
    private void OnZoomSliderChanged(float value)
    {
        zoomLevel = value;
        
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = zoomLevel;
        }
    }
    
    public void ToggleMinimap()
    {
        isMinimapActive = !isMinimapActive;
        
        if (minimapPanel != null)
        {
            minimapPanel.SetActive(isMinimapActive);
        }
        
        if (minimapCamera != null)
        {
            minimapCamera.enabled = isMinimapActive;
        }
    }
    
    public void AddIcon(GameObject target, GameObject iconPrefab)
    {
        if (iconPrefab != null && target != null)
        {
            GameObject icon = Instantiate(iconPrefab, minimapImage.transform);
            MinimapIcon iconScript = icon.AddComponent<MinimapIcon>();
            iconScript.target = target;
            iconScript.minimapCamera = minimapCamera;
            
            minimapIcons.Add(icon);
        }
    }
    
    public void RemoveIcon(GameObject icon)
    {
        if (minimapIcons.Contains(icon))
        {
            minimapIcons.Remove(icon);
            Destroy(icon);
        }
    }
    
    public void ClearAllIcons()
    {
        foreach (GameObject icon in minimapIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        
        minimapIcons.Clear();
    }
}

// 미니맵 아이콘 추적 스크립트
public class MinimapIcon : MonoBehaviour
{
    public GameObject target;
    public Camera minimapCamera;
    
    void Update()
    {
        if (target != null && minimapCamera != null)
        {
            Vector3 screenPos = minimapCamera.WorldToScreenPoint(target.transform.position);
            transform.position = screenPos;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}