using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;//引用系統集合、管理API(協同程式:非同步處理)
using UnityEngine.Events; //引用 事件API 寫出與OnClick  一樣的功能

public class Base : MonoBehaviour
{
    [Header("移動速度"), Range(0, 1000)]
    public float speed = 10;
    [Header("受傷事件")]
    public UnityEvent OnHit;
    [Header("跳躍高度"), Range(0, 1000)]
    public float jump = 100;
    [Header("血量"), Range(0, 1000)]
    public float hp = 100;
    [Header("攻擊力"), Range(0, 1000)]
    public float attack = 10;
    [Header("旋轉速度"), Range(0, 1000)]
    public float turn = 5;
    [Header("上下旋轉靈敏度"), Range(0, 100)]
    public float mouseUpDown = 1.5f;
    [Header("目標物件上下範圍限制")]
    public Vector2 v2TargetLimit = new Vector2(0, 3);
    [Header("發射子彈的位置")]
    public Transform traFirePoint;
    [Header("子彈預置物")]
    public GameObject objBullet;
    [Header("子彈發射速度"),Range(0,3000)]
    public float speedBullet = 600;
    [Header("子彈發射間隔"), Range(0,3)]
    public float IntervalFire = 0.5f;
    [Header("開槍音效")]
    public AudioClip soundFire;
    public AudioClip soundFireEmpty;
    [Header("檢查地板")]
    public float groundRadius = 0.5f;
    [Header("跳躍後恢復權重的時間")]
    public float timeRestoreWeight = 1.3f;
    [Header("人物類型")]
    public PeopleType type;


    private float hpMax;
    private Animator ani;
    private Rigidbody rig;
    private AudioSource aud;
    private Rig rigging;
    Vector3 posRig;
    public Vector3 groundOffest;
    private bool isGround;
    /// <summary>
    /// 目標物件
    /// </summary>
    [HideInInspector]
    public Transform traTarget;
    /// <summary>
    /// 開槍用計時器
    /// </summary>
    private float timerFire;
    /// <summary>
    /// 子彈目前數量
    /// </summary>
    public int bulletCurrect = 30;
    /// <summary>
    /// 彈夾數量
    /// </summary>
    private int bulletClip = 30;
    /// <summary>
    /// 子彈總數
    /// </summary>
    public int bulletTotal = 120;
    /// <summary>
    /// 是否死亡 : 死亡後記錄腳色是否死亡
    /// </summary>
    public bool dead;
    public AudioClip soundHit;
    public AudioClip soundHeadShot;


    private void Start()
    {
        ani = GetComponent<Animator>();
        rig = GetComponent<Rigidbody>();
        aud = GetComponent<AudioSource>();

        traTarget = transform.Find("目標物件");
        rigging = transform.Find("設置物件").GetComponent<Rig>();
    }

    /// <summary>
    /// 受傷
    /// </summary>
    /// <param name="damage"></param>
    private void Hit(float damage,AudioClip sound)
    {
        hp -= damage;
        aud.PlayOneShot(sound, Random.Range(0.8f, 1.2f));
        if (hp <= 0) Dead();

        OnHit.Invoke();
    }
    /// <summary>
    /// 死亡 : 動畫
    /// </summary>
    private void Dead()
    {
        hp = 0;
        ani.SetBool("死亡開關", true);
        rigging.weight = 0;
        dead = true;
        //關閉碰撞避免重複死亡判定與子彈碰撞
        GetComponent<SphereCollider>().enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;
        //剛體加速度歸零，並約束所有
        rig.velocity = Vector3.zero;
        rig.constraints = RigidbodyConstraints.FreezeAll;

        GameManager.instance.SomeBodyDead(type);
        enabled = false;
    }

    /// <summary>
    /// 移動
    /// </summary>
    public void Move(Vector3 movePosition)
    {
        //剛體.移動座標(物件座標 + 移動座標 * 速度)
        rig.MovePosition(transform.position + movePosition * speed);
    }
    public void Turn(float turnValueY,float moveTarget)
    {
        //變形.旋轉(上方 * 旋轉值 * 旋轉速度)
        transform.Rotate(transform.up * turnValueY * turn * Time.deltaTime);
        //目標物件.位移(x,y,z)
        traTarget.Translate(0, moveTarget * mouseUpDown * Time.deltaTime, 0);
        //取得目標物間區域座標 並限制在範圍內 最後更新座標
        Vector3 posTarget = traTarget.localPosition;
        posTarget.y = Mathf.Clamp(posTarget.y, v2TargetLimit.x, v2TargetLimit.y);
        traTarget.localPosition = posTarget;
    }
    private void Update()
    {
        // AnimatorMove();
        checkground();
    }
    private void AnimatorMove()
    {
        ani.SetBool("走路開關", rig.velocity.x !=0 || rig.velocity.z !=0);
    }
    public void Fire()
    {
        if(ani.GetBool("換彈匣"))
        {
            return;
        }
        if (timerFire < IntervalFire) timerFire += Time.deltaTime;
        else
        {
            if(bulletCurrect>0)
            {
                rigging.weight = 1;
                bulletCurrect--;
                timerFire = 0;
                aud.PlayOneShot(soundFire, Random.Range(0.5f, 1.2f));
                GameObject tempBullet = Instantiate(objBullet, traFirePoint.position, Quaternion.identity);

                //添加子彈腳本，並賦予攻擊力
                tempBullet.AddComponent<Bullet>().attack = attack;
                //忽略子彈與開槍者的碰撞
                Physics.IgnoreCollision(GetComponent<Collider>(), tempBullet.GetComponent<Collider>());

                tempBullet.GetComponent<Rigidbody>().AddForce(-traFirePoint.forward * speedBullet);
                
            }
            else
            {
                rigging.weight = 0;
                aud.PlayOneShot(soundFireEmpty, Random.Range(0.5f, 1.2f));
                timerFire = 0;
            }
        }
    }
    public void RelordBullet()
    {
        if(bulletCurrect == bulletClip || bulletTotal ==0)
        {
            return; //如果 目前子彈 等於 彈匣 或者 為零 就跳出 -- 不要補子彈
        }

        StartCoroutine(Relording());

        int bulletGetCount = bulletClip - bulletCurrect; //計算取出數量 = 但夾 -目前

        if(bulletTotal >= bulletGetCount)
        {
            bulletTotal -= bulletGetCount;    //總數 - 取出數量
            bulletCurrect += bulletGetCount;  //目前 + 取出數量
        }
        else
        {
            bulletCurrect += bulletTotal;
            bulletTotal =  0;
        }
    }

    private IEnumerator Relording()
    {
        ani.SetBool("換彈匣", true);

        yield return new WaitForSeconds(ani.GetCurrentAnimatorStateInfo(0).length * 0.8f);

        ani.SetBool("換彈匣", false);
    }

    public void Jump()
    {
        if(isGround)
        {
            CancelInvoke("RestoreWeight");
            Invoke("RestorWeight", timeRestoreWeight);
        }
        rig.AddForce(0, jump, 0);
       
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position + groundOffest, groundRadius);
    }
    /// <summary>
    /// 檢查地板，並且控制跳躍動畫，在地面上不跳躍，不再地面上就跳躍
    /// </summary>
    private void checkground()
    {
      Collider[] hit =  Physics.OverlapSphere(transform.position + groundOffest, groundRadius, 1 << 8);
      //如果 碰撞陣列數量 > 0 並且 碰撞物件名稱為地板 就代表在地板上 否則 就代表不再地板上
      isGround = hit.Length > 0 && hit[0].name =="地板" ? true:false;
      ani.SetBool("跳躍開關", !isGround);
    }

    private void RestoreWeight()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        //如果 碰到物件名稱.包含 子彈 就受傷
        if(collision.gameObject.name.Contains("子彈"))
        {
            //如果 自身被碰到的  碰撞類型 是球體就爆頭
            if (collision.contacts[0].thisCollider.GetType() == typeof(SphereCollider)) Hit(100, soundHeadShot);
            //否則就傳回子彈傷害
            else Hit(collision.gameObject.GetComponent<Bullet>().attack,soundHit);


        }
    }
}
