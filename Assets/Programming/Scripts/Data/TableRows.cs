/// <summary>
/// 기획 테이블 행 정의
///
/// Tables/*.xlsx 의 1행 키와 필드 이름이 같아야 한다 (TableConverterMenu 가 이름으로 대조)
/// 변환된 JSON 은 Assets/Planning 에 있고 GameTables 가 불러온다
/// </summary>

// 아이템 공통 열
[System.Serializable]
public class ItemRow
{
    public string     Id = "";
    public string     Name = "";
    public string     Description = "";
    public ItemRarity Rarity = ItemRarity.Common;
    public int        nWidth = 1;
    public int        nHeight = 1;
    public int        nPrice = 0;
    public bool       bDroppable = true;
    public bool       bStackable = false;
    public int        nMaxStack = 1;
}

// Weapon.xlsx
[System.Serializable]
public class WeaponRow : ItemRow
{
    public WeaponType   WeaponType = WeaponType.Sword;
    public WeaponRarity WeaponRarity = WeaponRarity.Common;
    public float        fBaseDamage = 0f;
    public float        fAttackSpeed = 1f;
    public float        fReach = 1f;
    public float        fAttackWidth = 0.5f;
    public float        fAttackLockTime = 0f;
}

// Armor.xlsx
[System.Serializable]
public class ArmorRow : ItemRow
{
    public ArmorSlot   ArmorSlot = ArmorSlot.Chest;
    public float       fDefenseBonus = 0f;
    public float       fMaxHealthBonus = 0f;
    public RuneElement RuneSlot1 = RuneElement.None;
}

// Enhance.xlsx (강화 단계별 1행)
[System.Serializable]
public class EnhanceRow
{
    public int   nLevel = 0;
    public float fSuccessRate = 0f;
    public float fAttackMultiplier = 1f;
    public float fSpeedMultiplier = 1f;
}

// JsonUtility 용 감싸기 (최상위 배열을 직접 읽지 못함)
[System.Serializable]
public class WeaponTable
{
    public WeaponRow[] rows = new WeaponRow[0];
}

[System.Serializable]
public class ArmorTable
{
    public ArmorRow[] rows = new ArmorRow[0];
}

[System.Serializable]
public class EnhanceTable
{
    public EnhanceRow[] rows = new EnhanceRow[0];
}
