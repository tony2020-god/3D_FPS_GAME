using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;//引用系統集合、管理API(協同程式:非同步處理)

/// <summary>
/// 遊戲管理器
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("結束畫面:群組")]
    public CanvasGroup groupFinal;
    /// <summary>
    /// 此類別的實體物件
    /// </summary>
    public static GameManager instance;
    /// <summary>
    /// AI總數
    /// </summary>
    private int countAI;
    /// <summary>
    /// 死亡的ai數量
    /// </summary>
    private int DeadAI;
    private Text textTitleFinal;
    public static bool isGameOver;
    /// <summary>
    /// 有人死亡，判定此類型死亡後要做得處理
    /// </summary>
    /// <param name="type"></param>
    public void SomeBodyDead(PeopleType type)
    {
        switch (type)
        {
            case PeopleType.player:
                StartCoroutine(ShowFinal("失敗"));
                break;
            case PeopleType.ai:
                DeadAI++;
                if (DeadAI == countAI) StartCoroutine(ShowFinal("勝利"));
                break;
            default:
                break;
        }
    }
    public void Start()
    {
        instance = this;
        isGameOver = false;
        //取得場景內所有貼 敵人 標籤 的物件總數
        countAI = GameObject.FindGameObjectsWithTag("敵人").Length;
        textTitleFinal = GameObject.Find("標題").GetComponent<Text>();
    }
    private IEnumerator ShowFinal(string title)
    {
        isGameOver = true;
        textTitleFinal.text = title;
        for (int i =0; i < 20; i++)
        {
            
            groupFinal.alpha += 1 / 40f;
            yield return new WaitForSeconds(0.02f);
        }
    }
    private void Update()
    {
        Replay();
        QuitGame();
    }
    private void Replay()
    {
        if (isGameOver && Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene("遊戲場景");
    }
    private void QuitGame()
    {
        if (isGameOver && Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }
}
/// <summary>
/// 人的類型:玩家或電腦
/// </summary>
public enum PeopleType
{
    player, ai
}
