using UnityEngine;
using TMPro;

public class ResourceInventoryPanel : MonoBehaviour
{
    // Inspector에서 불/물/바람/땅/어둠 순서로 5개 연결
    [SerializeField] TMP_Text[] amountTexts;

    public void Refresh()
    {
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
            int amount = ResourceInventory.Instance.GetResourceAmount(types[i]);
            amountTexts[i].text = $"{amount} / 99";
            amountTexts[i].color = amount >= 99 ? Color.red : Color.white;
        }
    }
}