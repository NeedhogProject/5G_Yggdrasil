using UnityEngine;

public class BaseObject
{
    private void Awake()
    {
        Init();
    }
    protected virtual void Init(){}   
}
