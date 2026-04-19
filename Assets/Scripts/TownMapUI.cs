using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TownMapUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mapPanel;
    public Image mapImage;
    public CanvasGroup canvasGroup;
    
    [Header("Map Settings")]
    public Sprite townMapSprite;
    public float fadeSpeed = 5f;
    
    [Header("Location Markers")]
    public Transform markerContainer;
    public GameObject locationMarkerPrefab;
    public List<LocationMarker> locationMarkers = new List<LocationMarker>();
    
    [Header("Player Marker")]
    public GameObject playerMarker;
    public Transform player;
    public float markerUpdateInterval = 0.1f;
    
    [Header("UI Elements")]
    public Button closeButton;
    public TMP_Text locationNameText;
    public TMP_Text locationDescriptionText;
    public GameObject locationInfoPanel;
    
    [Header("Map Locations")]
    public List<TownLocation> townLocations = new List<TownLocation>();
    
    private bool isMapOpen = false;
    private TownLocation selectedLocation;
    private float lastMarkerUpdate;
    
    void Start()
    {
        InitializeMap();
        SetupUI();
        mapPanel.SetActive(false);
    }
    
    void Update()
    {
        // M 키로 맵 토글
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
        
        // ESC로 맵 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && isMapOpen)
        {
            CloseMap();
        }
        
        // 플레이어 마커 업데이트
        if (isMapOpen && Time.time - lastMarkerUpdate > markerUpdateInterval)
        {
            UpdatePlayerMarker();
            lastMarkerUpdate = Time.time;
        }
    }
    
    private void InitializeMap()
    {
        // 맵 이미지 설정
        if (mapImage != null && townMapSprite != null)
        {
            mapImage.sprite = townMapSprite;
        }
        
        // 캔버스 그룹 초기화
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // 위치 마커 생성
        CreateLocationMarkers();
        
        // 정보 패널 숨기기
        if (locationInfoPanel != null)
        {
            locationInfoPanel.SetActive(false);
        }
    }
    
    private void SetupUI()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMap);
        }
    }
    
    private void CreateLocationMarkers()
    {
        if (locationMarkerPrefab == null || markerContainer == null) return;
        
        foreach (TownLocation location in townLocations)
        {
            GameObject markerObj = Instantiate(locationMarkerPrefab, markerContainer);
            LocationMarker marker = markerObj.GetComponent<LocationMarker>();
            
            if (marker != null)
            {
                marker.Initialize(location, this);
                marker.SetPosition(location.mapPosition);
                locationMarkers.Add(marker);
            }
        }
    }
    
    public void ToggleMap()
    {
        if (isMapOpen)
        {
            CloseMap();
        }
        else
        {
            OpenMap();
        }
    }
    
    public void OpenMap()
    {
        if (isMapOpen) return;
        
        mapPanel.SetActive(true);
        isMapOpen = true;
        
        StartCoroutine(FadeIn());
        
        Time.timeScale = 0f; // 게임 일시정지
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseMap()
    {
        if (!isMapOpen) return;
        
        StartCoroutine(FadeOut());
        
        Time.timeScale = 1f; // 게임 재개
        
        HideLocationInfo();
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.unscaledDeltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private System.Collections.IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.unscaledDeltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        mapPanel.SetActive(false);
        isMapOpen = false;
    }
    
    private void UpdatePlayerMarker()
    {
        if (player == null || playerMarker == null) return;
        
        // 플레이어 월드 좌표를 맵 좌표로 변환
        Vector2 mapPosition = WorldToMapPosition(player.position);
        playerMarker.GetComponent<RectTransform>().anchoredPosition = mapPosition;
    }
    
    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        // 월드 좌표를 맵 UI 좌표로 변환
        // 실제 맵 크기와 게임 월드 크기에 따라 조정 필요
        
        float mapWidth = mapImage.rectTransform.rect.width;
        float mapHeight = mapImage.rectTransform.rect.height;
        
        // 예시: 100x100 월드를 맵에 매핑
        float worldSize = 100f;
        
        float normalizedX = (worldPosition.x + worldSize / 2f) / worldSize;
        float normalizedZ = (worldPosition.z + worldSize / 2f) / worldSize;
        
        float mapX = (normalizedX - 0.5f) * mapWidth;
        float mapY = (normalizedZ - 0.5f) * mapHeight;
        
        return new Vector2(mapX, mapY);
    }
    
    public void OnLocationMarkerClicked(TownLocation location)
    {
        selectedLocation = location;
        ShowLocationInfo(location);
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
    }
    
    private void ShowLocationInfo(TownLocation location)
    {
        if (locationInfoPanel == null) return;
        
        locationInfoPanel.SetActive(true);
        
        if (locationNameText != null)
        {
            locationNameText.text = location.locationName;
        }
        
        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = location.description;
        }
    }
    
    private void HideLocationInfo()
    {
        if (locationInfoPanel != null)
        {
            locationInfoPanel.SetActive(false);
        }
        
        selectedLocation = null;
    }
    
    public void TeleportToLocation()
    {
        if (selectedLocation == null || player == null) return;
        
        // 텔레포트 가능 여부 확인
        if (!selectedLocation.canTeleport)
        {
            Debug.Log("이 위치로는 이동할 수 없습니다.");
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }
        
        // 플레이어를 해당 위치로 이동
        player.position = selectedLocation.worldPosition;
        
        CloseMap();
        
        AudioManager.Instance?.PlaySFX(SFXClip.DoorOpen);
        
        Debug.Log($"{selectedLocation.locationName}(으)로 이동했습니다.");
    }
    
    public void UnlockLocation(string locationName)
    {
        TownLocation location = townLocations.Find(loc => loc.locationName == locationName);
        
        if (location != null && !location.isUnlocked)
        {
            location.isUnlocked = true;
            
            // 해당 마커 업데이트
            LocationMarker marker = locationMarkers.Find(m => m.location == location);
            if (marker != null)
            {
                marker.UpdateVisibility();
            }
            
            Debug.Log($"{locationName} 위치가 잠금 해제되었습니다!");
        }
    }
    
    public void HighlightLocation(string locationName)
    {
        foreach (LocationMarker marker in locationMarkers)
        {
            if (marker.location.locationName == locationName)
            {
                marker.Highlight(true);
            }
            else
            {
                marker.Highlight(false);
            }
        }
    }
}

// 마을 위치 데이터
[System.Serializable]
public class TownLocation
{
    public string locationName;
    public string description;
    public Vector2 mapPosition; // 맵 UI 상의 위치
    public Vector3 worldPosition; // 실제 게임 월드 위치
    public Sprite locationIcon;
    public LocationType locationType;
    public bool isUnlocked = true;
    public bool canTeleport = false;
}

public enum LocationType
{
    Shop,           // 상점
    Blacksmith,     // 대장간
    InscriptionMaster, // 각인술사
    Scholar,        // 학자
    Inn,            // 여관
    DungeonEntrance, // 던전 입구
    Plaza,          // 광장
    Other           // 기타
}

// 위치 마커 컴포넌트
public class LocationMarker : MonoBehaviour
{
    [Header("UI References")]
    public Image markerImage;
    public Image highlightImage;
    public TMP_Text locationNameText;
    public Button markerButton;
    
    [Header("Marker Settings")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color lockedColor = Color.gray;
    
    [HideInInspector]
    public TownLocation location;
    
    private TownMapUI townMapUI;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (markerButton != null)
        {
            markerButton.onClick.AddListener(OnMarkerClicked);
        }
        
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }
    
    public void Initialize(TownLocation loc, TownMapUI mapUI)
    {
        location = loc;
        townMapUI = mapUI;
        
        // 마커 아이콘 설정
        if (markerImage != null && location.locationIcon != null)
        {
            markerImage.sprite = location.locationIcon;
        }
        
        // 위치 이름 설정
        if (locationNameText != null)
        {
            locationNameText.text = location.locationName;
        }
        
        UpdateVisibility();
    }
    
    public void SetPosition(Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }
    
    public void UpdateVisibility()
    {
        if (markerImage != null)
        {
            if (location.isUnlocked)
            {
                markerImage.color = normalColor;
                markerButton.interactable = true;
            }
            else
            {
                markerImage.color = lockedColor;
                markerButton.interactable = false;
            }
        }
    }
    
    public void Highlight(bool highlight)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(highlight);
        }
        
        if (markerImage != null && highlight)
        {
            markerImage.color = highlightColor;
        }
        else if (markerImage != null)
        {
            markerImage.color = normalColor;
        }
    }
    
    private void OnMarkerClicked()
    {
        if (townMapUI != null && location != null)
        {
            townMapUI.OnLocationMarkerClicked(location);
        }
    }
}