/// <summary>
/// 방어구 각인 조합 서명 — ArmorInstance 세트 집계에 사용
/// (RuneSlot1, RuneSlot2) 조합을 값으로 비교하기 위한 구조체
/// </summary>
public struct SetSignature : System.IEquatable<SetSignature>
{
    public readonly RuneElement Slot1;
    public readonly RuneElement Slot2;

    public SetSignature(RuneElement slot1, RuneElement slot2)
    {
        Slot1 = slot1;
        Slot2 = slot2;
    }

    public bool Equals(SetSignature other) =>
        Slot1 == other.Slot1 && Slot2 == other.Slot2;

    public override bool Equals(object obj) =>
        obj is SetSignature other && Equals(other);

    public override int GetHashCode() =>
        System.HashCode.Combine(Slot1, Slot2);

    public override string ToString() => $"{Slot1}/{Slot2}";
}
