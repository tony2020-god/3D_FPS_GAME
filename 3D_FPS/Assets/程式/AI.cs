using UnityEngine;
using UnityEngine.AI; //引用 AI API

/// <summary>
/// 敵人 AI :決定如何移動、追蹤玩家、開槍跳躍受傷
/// </summary>
public class AI : MonoBehaviour
{
    #region 欄位
    /// <summary>
    /// AI 狀態
    /// </summary>
    public StateAI state;
  
    [Header("旋轉面相物件的速度"),Range(0,100)]
    private float turnSpeed = 3;
    private Transform player;
    [Header("每次開槍後的偏差值")]
    private float offsetFire = 0.05f;
    public Vector2 v2FireLmit = new Vector2(0.8f, 1.2f);
    [Header("等待幾秒後進入隨機行走")]
    public Vector2 v2IdleToRandomWalk = new Vector2(2, 6);
    [Header("隨機走動半徑"), Range(0, 100)]
    public float radiusRandomWalk = 20;
    [Header("移動到隨機座標的停止距離")]
    public float distanceStop = 1.5f;
    [Header("隨機走動後隨機等待的機率")]
    public float idleProbility = 0.3f;
    [Header("檢查是否看到玩家的資訊: 方體前方位移以及方體的尺寸")]
    public float checkCubeOffsetForward = 5f;
    [Header("與玩家夾角為幾度就開槍")]
    public float angleFire = 2f;
    [Header("開槍準心校正間隔")]
    public float intervalFire =0.2f;
 
    /// <summary>
    /// 基底類別
    /// </summary>
    private Base basePerson;
    /// <summary>
    /// 導覽網路 代理器
    /// </summary>
    private NavMeshAgent agent;
    /// <summary>
    ///隨機走動使用的隨機座標
    /// </summary>
    private Vector3 posRandom;
    /// <summary>
    /// 是否從待機前往隨機走動 - 預設為沒有
    /// </summary>
    private bool isWaitToRandomWalk;
    /// <summary>
    /// 是否在隨機走動中
    /// </summary>
    private bool randomWalking = false;
    /// <summary>
    /// 導覽網格碰撞 -在網格內儲存隨機座標
    /// </summary>
    private NavMeshHit hitRandomWalk;
    //介於隨機座標與角色座標之間的位置 - 要移動到的座標
    private Vector3 posMove;
    private float timerfire;
    public Vector3 checkCubeSize = new Vector3(1, 1, 10);
    private Base bassPerson;
    #endregion

    #region 方法
    /// <summary>
    /// 檢查狀態並決定執行哪一行為
    /// </summary>
    private void CheckState()
    {
        switch (state)
        {
            case StateAI.Idle:
                Idle();
                break;
            case StateAI.RandomWalk:
                RandomWalk();
                break;
            case StateAI.TrackTarget:
                TrackerTarget();
                break;
            case StateAI.Fire:
                Fire();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 隨機走動，在角色半徑以內擷取隨機座標並移動
    /// </summary>
    private void RandomWalk()
    {
        //如果還沒進行隨機走路，就取得隨機座標
        if(!randomWalking)
        {
            print("隨機走動中");
            //隨機座標 = 隨機球體座標 * 半徑 + 角色中心點 --以角色為中心選擇半徑內的隨機座標
            posRandom = Random.insideUnitSphere * radiusRandomWalk + transform.position;
            //網格.取得樣本座標(隨機座標，在網格內的隨機座標，半徑，區域)
            NavMesh.SamplePosition(posRandom, out hitRandomWalk, radiusRandomWalk, NavMesh.AllAreas);
            posRandom = hitRandomWalk.position;
          
            randomWalking = true;
        } 
        //否則 正在移動中 取得前方座標 並前往移動 - 前往移動在FixedUpDate中處理
        else if(randomWalking)
        {
            //如果 與隨機座標的距離 > 停止距離 就繼續移動
            if (Vector3.Distance(transform.position,posRandom)>distanceStop)
            {
                //當前座標與隨機座標之間的位置 - 取得前方的位置
                posMove = Vector3.MoveTowards(transform.position, posRandom, 1);

                LookTargetSmooth(posMove);
            }
            //否則 就決定處理下一個狀態 - 隨機、等待 或隨機走路
            else
            {
                float r = Random.Range(0f, 1f);
                if(r < idleProbility)
                {
                    state = StateAI.Idle;
                }
                else
                {
                    state = StateAI.RandomWalk;
                }
                randomWalking = false;
            }
        }
    }

    /// <summary>
    /// 待機:隨機秒數後開始走動
    /// </summary>
    private void Idle()
    {
        if(!isWaitToRandomWalk) //如果 不是等待前往隨機走動
        {
            float random = Random.Range(v2IdleToRandomWalk.x, v2IdleToRandomWalk.y); //取得隨機秒數
            isWaitToRandomWalk = true;                                               //已經在等待前往隨機走動 中
            CancelInvoke("IdleWaitToRandomWalk");
            Invoke("IdleWaitToRandomWalk", random);        
        }
    }

    /// <summary>
    /// 待機等待前往隨機走路
    /// </summary>
    private void IdleWaitToRandomWalk()
    {
        state = StateAI.RandomWalk;
        isWaitToRandomWalk = false;
    }

    private void Start()
    {
        basePerson = GetComponent<Base>();
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.Find("玩家").transform;
    }

    private void Update()
    {
        if (basePerson.dead) return;
        CheckState();
        //如果 玩家進入到檢查立方體內 並且 不是在開槍狀態 就進入 追蹤狀態
        if (CheckPlayerInCude() && state != StateAI.Fire) state = StateAI.TrackTarget;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radiusRandomWalk);

        Gizmos.color = new Color(0, 0.8f, 1, 0.8f);
        Gizmos.DrawSphere(posRandom, 0.5f);

        Gizmos.color = new Color(1, 0, 0, 0.8f);
        Gizmos.DrawSphere(posMove, 0.6f);

        Gizmos.color = new Color(0.8f, 0.1f, 0.1f,0.3f);
        //矩陣 = 矩陣.座標角度尺寸(座標、角度、尺寸)
        Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.forward * checkCubeOffsetForward, transform.rotation, transform.localScale);
        Gizmos.DrawCube(Vector3.zero, checkCubeSize);
          
    }
    private void FixedUpdate()
    {
        if (basePerson.dead) return;
        if (randomWalking)
        {
            basePerson.Move(transform.forward);
        }
    }
    //平滑的面相物標物件
    private Quaternion LookTargetSmooth(Vector3 posTarget)
    {
        //計算目與此物件的面項角度
        Quaternion quaLook = Quaternion.LookRotation(posTarget - transform.position);
        //角度的插值
        transform.rotation = Quaternion.Lerp(transform.rotation, quaLook, turnSpeed * Time.deltaTime);
        //傳回敵人與玩家的角度
        return quaLook;
    }
    /// <summary>
    /// 檢查玩家是否進入到立方體內
    /// </summary>
    /// <returns></returns>
    private bool CheckPlayerInCude()
    {
        Collider[] hit =Physics.OverlapBox(transform.position + transform.forward * checkCubeOffsetForward, checkCubeSize / 2,Quaternion.identity,1<<9);
        bool playerInCude = false;
        if (hit.Length > 0) playerInCude = true;
        else playerInCude = false;

        return playerInCude;
    }
    private void TrackerTarget()
    {
        randomWalking = false;
        Quaternion angleLook = LookTargetSmooth(player.position);
        float angle = Quaternion.Angle(transform.rotation, angleLook);
        randomWalking = false;
        if (angle <= angleFire) state = StateAI.Fire;
    }
    /// <summary>
    /// 開槍
    /// </summary>
    private void Fire()
    {
        LookTargetSmooth(player.position);
        //如果 子彈 數量 = 0
        if(basePerson.bulletCurrect == 0)
        {
            basePerson.RelordBullet(); //換子彈
        }
        else
        {
            basePerson.Fire();       //否則就開槍

            if (timerfire <= intervalFire)
            {
                timerfire += Time.deltaTime;
            }
            else
            {
                Vector3 posTargetPoint = basePerson.traTarget.localPosition;
                posTargetPoint.y += (float)(Random.Range(-1, 2) * offsetFire);
                posTargetPoint.y = Mathf.Clamp(posTargetPoint.y, v2FireLmit.x, v2FireLmit.y);
                basePerson.traFirePoint.localPosition = posTargetPoint;
                timerfire = 0;
            }
        }     
    }
    #endregion
}

/// <summary>
/// AI 狀態 : 待機、隨機走動、追蹤目標物、開槍
/// </summary>
public enum StateAI
{
    Idle, RandomWalk, TrackTarget, Fire
}
