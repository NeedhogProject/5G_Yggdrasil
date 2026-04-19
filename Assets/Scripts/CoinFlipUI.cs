using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CoinFlipUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject coinObject;
    public Image coinImage;
    public Sprite headsSprite; // 앞면
    public Sprite tailsSprite; // 뒷면
    
    [Header("Animation Settings")]
    public float flipDuration = 2f;
    public float flipSpeed = 20f;
    public AnimationCurve flipCurve;
    
    [Header("Result Display")]
    public GameObject resultPanel;
    public Text resultText;
    public Color successColor = Color.green;
    public Color failColor = Color.red;
    
    private bool isFlipping = false;
    private bool flipResult = false;
    
    void Start()
    {
        coinObject.SetActive(false);
        resultPanel.SetActive(false);
    }
    
    public bool PlayCoinFlip(float successRate)
    {
        if (isFlipping) return false;
        
        // 결과 미리 결정
        flipResult = Random.value <= successRate;
        
        StartCoroutine(CoinFlipAnimation());
        
        return flipResult;
    }
    
    private IEnumerator CoinFlipAnimation()
    {
        isFlipping = true;
        coinObject.SetActive(true);
        resultPanel.SetActive(false);
        
        float elapsedTime = 0f;
        int flipCount = 0;
        
        AudioManager.Instance?.PlaySFX(SFXClip.CoinFlip);
        
        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 코인 회전
            float rotationProgress = elapsedTime / flipDuration;
            float currentSpeed = flipSpeed * flipCurve.Evaluate(rotationProgress);
            
            coinObject.transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime * 360f);
            
            // 앞뒷면 전환
            if (flipCount % 2 == 0)
            {
                coinImage.sprite = headsSprite;
            }
            else
            {
                coinImage.sprite = tailsSprite;
            }
            
            flipCount++;
            
            yield return null;
        }
        
        // 최종 결과 표시
        if (flipResult)
        {
            coinImage.sprite = headsSprite;
            ShowResult("성공!", successColor);
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
        }
        else
        {
            coinImage.sprite = tailsSprite;
            ShowResult("실패...", failColor);
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceFail);
        }
        
        yield return new WaitForSeconds(1f);
        
        coinObject.SetActive(false);
        resultPanel.SetActive(false);
        
        isFlipping = false;
    }
    
    private void ShowResult(string message, Color color)
    {
        resultPanel.SetActive(true);
        resultText.text = message;
        resultText.color = color;
    }
}