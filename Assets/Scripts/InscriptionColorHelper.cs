// InscriptionColorHelper.cs
// 각인 속성에 따라 아이콘 색상을 반환한다.

using UnityEngine;
using UnityEngine.UI;

public class InscriptionColorHelper : MonoBehaviour
{
    // 각 슬롯의 InscriptionIcon Image (인스펙터에서 연결)
    public Image weaponInscriptionIcon;
    public Image helmetInscriptionIcon;
    public Image chestInscriptionIcon;
    public Image legsInscriptionIcon;
    public Image bootsInscriptionIcon;

    // 속성별 색상
    private Color fireColor = new Color(1f, 0.3f, 0.1f, 1f); // 빨강
    private Color waterColor = new Color(0.1f, 0.5f, 1f, 1f); // 파랑
    private Color windColor = new Color(0.5f, 1f, 0.3f, 1f); // 초록
    private Color earthColor = new Color(0.6f, 0.4f, 0.1f, 1f); // 갈색
    private Color darknessColor = new Color(0.4f, 0.1f, 0.6f, 1f); // 보라
    private Color noneColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 회색 투명

    // 각인 변경 시 호출 (슬롯 이름 + 각인 속성 이름)
    public void UpdateInscriptionColor(string slotName, string runeType)
    {
        Image targetIcon = GetIconBySlot(slotName);

        if (targetIcon == null)
        {
            return;
        }

        targetIcon.color = GetColorByRune(runeType);
    }

    private Image GetIconBySlot(string slotName)
    {
        if (slotName == "Helmet") { return helmetInscriptionIcon; }
        if (slotName == "Chest") { return chestInscriptionIcon; }
        if (slotName == "Legs") { return legsInscriptionIcon; }
        if (slotName == "Boots") { return bootsInscriptionIcon; }
        return null;
    }

    private Color GetColorByRune(string runeType)
    {
        if (runeType == "불") { return fireColor; }
        if (runeType == "물") { return waterColor; }
        if (runeType == "바람") { return windColor; }
        if (runeType == "땅") { return earthColor; }
        if (runeType == "어둠") { return darknessColor; }
        return noneColor;
    }
}