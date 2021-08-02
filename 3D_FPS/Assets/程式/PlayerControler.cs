using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 玩家控制類別:玩家滑鼠、鍵盤的輸入資訊以及跟 Base 溝通
/// </summary>
public class PlayerControler : MonoBehaviour
{  
    /// <summary>
    /// 人物基底類別
    /// </summary>
    private Base basePerson;
    /// <summary>
    /// 要移動的座標資訊
    /// </summary>
    private Vector3 v3Move;
    /// <summary>
    /// 要旋轉的值
    /// </summary>
    private Vector3 v3Turn;
    /// <summary>
    /// 攝影機
    /// </summary>
    private Transform traCamera;
    /// <summary>
    /// 目前的子彈數量
    /// </summary>
    public Text textBulletCurrent;
    /// <summary>
    /// 子彈總數
    /// </summary>
    public Text textBulletTotal;
    /// <summary>
    /// 血量
    /// </summary>
    private Text textHp;
    private float hpMax;
    /// <summary>
    /// 血條
    /// </summary>
    private Image imgHp;
    private void Start()
    {
        Cursor.visible = false;
        basePerson = GetComponent<Base>();
        traCamera = transform.Find("攝影機");
        textBulletCurrent = GameObject.Find("目前子彈數量").GetComponent<Text>();
        textBulletTotal = GameObject.Find("子彈總數").GetComponent<Text>();
        imgHp = GameObject.Find("血條").GetComponent<Image>();
        textHp = GameObject.Find("血量").GetComponent<Text>();
        hpMax = basePerson.hp;
        UpdateUIBullet();
    }

    public void Hit()
    {
        imgHp.fillAmount = basePerson.hp / hpMax;
        textHp.text = "HP : " + basePerson.hp;
    }
    //固定更新事件:50fps 物理行為在此事件執行
    private void FixedUpdate()
    {
        if (basePerson.dead) return;
        //呼叫基底類別移動(傳入角色方向)
        basePerson.Move(transform.forward * v3Move.z + transform.right * v3Move.x);
    }
    private void Update()
    {
        if (basePerson.dead) return;

        GetMoveInput();
        GetTurnInput();
        TurnCamera();
        Fire();
        Relord();
        Jump();

        //呼叫基底類別 旋轉
        basePerson.Turn(v3Turn.y,v3Turn.x);
    }

    private void GetMoveInput()
    {
        float h = Input.GetAxis("Horizontal"); //水平值 A -1 , D 1
        float v = Input.GetAxis("Vertical");   //垂直 S -1 , W 1
        v3Move.x = h; //左右為x軸
        v3Move.z = v; //前後為z軸
    }
    private void GetTurnInput()
    {
        float mouseX = Input.GetAxis("Mouse X"); //取得滑鼠x值
        float mouseY = Input.GetAxis("Mouse Y"); //取得滑鼠y值 
        v3Turn.x = mouseY; //物件X軸對應滑鼠Y
        v3Turn.y = mouseX; //物件Y軸對應滑鼠X
    }
    private void TurnCamera()
    {
        traCamera.LookAt(basePerson.traTarget);
    }

    /// <summary>
    /// 玩家開槍的方式:按下左鍵
    /// </summary>
    private void Fire()
    {
        if(Input.GetKey(KeyCode.Mouse0))
        {
            basePerson.Fire();
            UpdateUIBullet();
        }
    }

    private void UpdateUIBullet()
    {
        textBulletCurrent.text = basePerson.bulletCurrect.ToString();
        textBulletTotal.text = "/" + basePerson.bulletTotal.ToString();
    }
    private void Relord()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            basePerson.RelordBullet();
            UpdateUIBullet();
        }
    }

    private void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            basePerson.Jump();
        }
    }
}
