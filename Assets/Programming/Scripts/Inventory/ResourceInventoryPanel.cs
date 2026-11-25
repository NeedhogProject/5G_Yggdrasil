using UnityEngine;
using TMPro;

public class ResourceInventoryPanel : MonoBehaviour
{
    // Inspector에서 불/물/바람/땅/어둠 순서로 5개 연결
    [SerializeField] TMP_Text[] amountTexts;

    // 패널이 켜질 때마다 현재 보유량 갱신 (탭 전환이 SetActive 방식이면 자동 동작)
    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        // 자원 인벤토리 인스턴스가 없으면 갱신 불가 (씬에 ResourceInventory 배치 확인)
        if (ResourceInventory.Instance == null)
        {
            Debug.LogWarning("[ResourceInventoryPanel] ResourceInventory.Instance 없음 — 갱신 불가");
            return;
        }

        // 텍스트 배열 미연결 방지
        if (amountTexts == null)
        {
            return;
        }

        var types = new InscriptionType[]
        {
            InscriptionType.Fire,
            InscriptionType.Water,
            InscriptionType.Wind,
            InscriptionType.Earth,
            InscriptionType.Darkness
        };

        for (int i = 0; i < amountTexts.Length; i++)
        {
            int amount = ResourceInventory.Instance.GetResourceCount(types[i]);
            amountTexts[i].text = $"{amount} / 99";
            amountTexts[i].color = amount >= 99 ? Color.red : Color.white;
        }
    }
}