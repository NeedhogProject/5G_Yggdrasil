using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIItemTooltip : MonoBehaviour
{
    public static UIItemTooltip Instance { get; private set; }
    
    [Header("UI References")]
    public GameObject tooltipPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemStatsText;
    public TMP_Text itemPriceText;
    public Image itemIconImage;
    public Image rarityBorder;
    
    [Header("Tooltip Settings")]
    public Vector2 offset = new Vector2(10, -10);
    public float followSpeed = 10f;
    
    private RectTransform tooltipRect;
    private Canvas canvas;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        HideTooltip();
    }
    
    void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // 마우스 따라다니기
            Vector2 mousePosition = Input.mousePosition;
            Vector2 targetPosition = mousePosition + offset;
            
            // 화면 밖으로 나가지 않도록 조정
            targetPosition = ClampToScreen(targetPosition);
            
            tooltipRect.position = Vector2.Lerp(tooltipRect.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
    
    public void ShowTooltip(ItemData item, Vector3 position)
    {
        if (item == null) return;
        
        tooltipPanel.SetActive(true);
        
        // 아이템 정보 표시
        itemNameText.text = item.itemName;
        itemTypeText.text = GetItemTypeName(item.itemType);
        itemDescriptionText.text = item.itemDescription;
        itemPriceText.text = $"가격: {item.basePrice} 골드";
        
        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.itemIcon;
        }
        
        // 희귀도에 따른 테두리 색상
        if (rarityBorder != null)
        {
            rarityBorder.color = GetRarityColor(item.rarity);
        }
        
        // 장비일 경우 능력치 표시
        if (item is EquipmentData equipment)
        {
            ShowEquipmentStats(equipment);
        }
        else if (item is RelicData relic)
        {
            ShowRelicStats(relic);
        }
        else
        {
            itemStatsText.text = "";
        }
        
        // 위치 설정
        tooltipRect.position = position + (Vector3)offset;
    }
    
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
    
    private void ShowEquipmentStats(EquipmentData equipment)
    {
        string stats = "";
        
        if (equipment.attack > 0)
        {
            stats += $"공격력: {equipment.attack}\n";
        }
        
        if (equipment.defense > 0)
        {
            stats += $"방어력: {equipment.defense}\n";
        }
        
        if (equipment.upgradeLevel > 0)
        {
            stats += $"강화: +{equipment.upgradeLevel}\n";
        }
        
        if (equipment.isInscribed)
        {
            stats += $"각인: {GetInscriptionName(equipment.inscriptionType)}\n";
        }
        
        stats += $"내구도: {equipment.durability:F0}/{equipment.maxDurability:F0}";
        
        itemStatsText.text = stats;
    }
    
    private void ShowRelicStats(RelicData relic)
    {
        if (relic.isIdentified)
        {
            itemStatsText.text = relic.relicEffect;
        }
        else
        {
            itemStatsText.text = "미확인 유물\n감정이 필요합니다.";
        }
    }
    
    private string GetItemTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment: return "장비";
            case ItemType.Consumable: return "소비 아이템";
            case ItemType.Relic: return "유물";
            case ItemType.Resource: return "자원";
            case ItemType.QuestItem: return "퀘스트 아이템";
            default: return "기타";
        }
    }
    
    private string GetInscriptionName(InscriptionType type)
    {
        switch (type)
        {
            case InscriptionType.Fire: return "불";
            case InscriptionType.Water: return "물";
            case InscriptionType.Wind: return "바람";
            case InscriptionType.Earth: return "땅";
            case InscriptionType.Darkness: return "어둠";
            default: return "없음";
        }
    }
    
    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return new Color(0.8f, 0.8f, 0.8f); // 회색
            case ItemRarity.Uncommon:
                return new Color(0.3f, 1f, 0.3f); // 초록색
            case ItemRarity.Rare:
                return new Color(0.3f, 0.5f, 1f); // 파란색
            case ItemRarity.Epic:
                return new Color(0.8f, 0.3f, 1f); // 보라색
            case ItemRarity.Legendary:
                return new Color(1f, 0.6f, 0f); // 주황색
            default:
                return Color.white;
        }
    }
    
    private Vector2 ClampToScreen(Vector2 position)
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        
        // 오른쪽 경계
        if (position.x + tooltipSize.x > screenSize.x)
        {
            position.x = screenSize.x - tooltipSize.x;
        }
        
        // 왼쪽 경계
        if (position.x < 0)
        {
            position.x = 0;
        }
        
        // 위쪽 경계
        if (position.y > screenSize.y)
        {
            position.y = screenSize.y;
        }
        
        // 아래쪽 경계
        if (position.y - tooltipSize.y < 0)
        {
            position.y = tooltipSize.y;
        }
        
        return position;
    }
}