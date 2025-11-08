using System;
using UnityEngine;
 
namespace TarodevController
{
    /// <summary>
    /// Tarodev 提供的 2D 玩家控制器
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]//挂载Rigidbody2D和Collider2D组件
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;        // 配置的行为参数
        private Rigidbody2D _rb;                                // 刚体组件引用
        private CapsuleCollider2D _col;                         // 碰撞器组件引用
        private FrameInput _frameInput;                         // 当前帧输入
        private Vector2 _frameVelocity;                         // 计算后帧速度
        private bool _cachedQueryStartInColliders;               // 缓存 Physics2D.queriesStartInColliders 原值
 
        #region Interface
 
        public Vector2 FrameInput => _frameInput.Move;         // 对外暴露的移动输入
        public event Action<bool, float> GroundedChanged;     // 地面状态改变事件，参数：是否着地，上一次离地时的垂直速度
        public event Action Jumped;                           // 跳跃事件
 
        #endregion
 
        private float _time;                                     // 游戏运行时间计数
 
        private void Awake()
        {
            // 缓存所有组件引用
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
 
            // 缓存物理查询设置
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }
 
        private void Update()
        {
            _time += Time.deltaTime;//累计时间
            GatherInput();//采集玩家输入
        }
 
        /// <summary>
        /// 收集玩家输入，并处理跳跃缓冲与死区
        /// </summary>
        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),     // 跳跃按下
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),            // 跳跃持续
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))// 水平与垂直移动输入
            };
 
            // 输入死区处理（可选）让
            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold//小于一定值则归零，防止因为误触而移动
                    ? 0 : Mathf.Sign(_frameInput.Move.x);//Mathf.Sign(x)返回x的正负号 这里用来规范到整数
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold
                    ? 0 : Mathf.Sign(_frameInput.Move.y);
            }
 
            // 跳跃输入缓冲
            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;          //是否有输入待处理
                _timeJumpWasPressed = _time;    //记录跳跃按下时刻
            }
        }
 
        private void FixedUpdate()
        {
            // 物理更新：检测碰撞、处理跳跃、方向、重力，最后应用速度
            CheckCollisions();//碰撞检测
            HandleJump();//跳跃处理
            HandleDirection();//水平移动处理
            HandleGravity();//重力处理
            ApplyMovement();//应用速度
        }
 
        #region Collisions
 
        private float _frameLeftGrounded = float.MinValue;       // 离地时间戳
        private bool _grounded;                                   // 是否着地
 
        /// <summary>
        /// 检测地面与天花板碰撞，并触发相应事件
        /// </summary>
        private void CheckCollisions()
        {
            // 禁止从内部开始查询碰撞，以避免自碰撞
            Physics2D.queriesStartInColliders = false;
 
            // 向下检测地面，向上检测天花板
            /*
            Physics2D.CapsuleCast(
                _col.bounds.center,        // 胶囊中心点（角色中心）
                _col.size,                 // 胶囊尺寸（和碰撞体一样大）
                _col.direction,            // 胶囊朝向（垂直 or 水平）
                0,                         // 胶囊体旋转角度（这里不旋转）
                Vector2.down,              // 投射方向 ↓（检测“地面”）
                _stats.GrounderDistance,   // 投射距离（短短的一点点）
                ~_stats.PlayerLayer        // 探测哪些层级（除了玩家自己）
            );
            */
            // 地面
            bool groundHit = Physics2D.CapsuleCast(
                _col.bounds.center, _col.size, _col.direction,
                0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer
            );
            //天花板
            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center, _col.size, _col.direction,
                0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer
            );
 
            // 撞到天花板时，限制向上速度
            if (ceilingHit)
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
 
            // 处理落地事件
            if (!_grounded && groundHit)//如果之前状态是不在地面，并且下方检测到地面
            {
                _grounded = true;//修改状态为在在地面
                _coyoteUsable = true;//土狼跳跃(离开地面后仍然能起跳)//可用
                _bufferedJumpUsable = true;//跳跃缓冲可用 
                _endedJumpEarly = false;//提前松开跳跃
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));//触发落地事件广播(用于动画控制)，传递落地时候的速度
            }
            // 离地事件
            else if (_grounded && !groundHit)//如果之前状态是在地面，并且下方没有检测到地面
            {
                _grounded = false;//不再着地
                _frameLeftGrounded = _time;//记录离地时间戳
                GroundedChanged?.Invoke(false, 0);//触发离地事件(用于动画控制)，传递上一次离地时的速度
            }
 
            // 恢复原查询设置
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }
 
        #endregion
 
        #region Jumping
 
        private bool _jumpToConsume;                             // 是否有跳跃输入待处理
        private bool _bufferedJumpUsable;                        // 缓冲跳跃是否可用
        private bool _endedJumpEarly;                            // 是否提前松开跳跃
        private bool _coyoteUsable;                              // 是否可使用 coyote jump
        private float _timeJumpWasPressed;                       // 跳跃按下时刻
 
        // 跳跃缓冲有效
        //(使用属性，每次访问都是重新计算)
        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;//是在落地的一瞬间_bufferedJumpUsable为true，才计算这个状态。
        // Coyote 时间内可跳
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;
 
        /// <summary>
        /// 处理跳跃输入、缓冲与 coyote jump
        /// </summary>
        private void HandleJump()
        {
            // 检测跳跃提前结束，用于调整重力
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0)//没有标记为提前结束，并且不在地面上，并且跳跃键没有一直按住，并且速度向上。
                _endedJumpEarly = true;
 
            // 无待处理跳跃且缓冲已过期，直接返回
            if (!_jumpToConsume && !HasBufferedJump)//如果没有待处理的跳跃则返回
                return;
 
            if (_grounded || CanUseCoyote)//如果可以有跳跃需处理 在地面上或者满足土狼跳(刚离开地面没多久)的条件则执行跳跃
                ExecuteJump();
 
            _jumpToConsume = false;//清空跳跃请求状态
        }
 
        /// <summary>
        /// 执行跳跃逻辑，重置相关状态
        /// </summary>
        private void ExecuteJump()
        {
            // 重置相关状态
            _endedJumpEarly = false;//将提前结束标记清空
            _timeJumpWasPressed = 0;//将跳跃按下时刻清空
            _bufferedJumpUsable = false;//缓冲跳跃状态为不可用
            _coyoteUsable = false;//土狼跳状态为不可用
            _frameVelocity.y = _stats.JumpPower;//设置跳跃速度
            //跳跃事件广播(用于角色动画代码)
            Jumped?.Invoke();
        }
 
        #endregion
 
        #region Horizontal
 
        /// <summary>
        /// 处理水平移动和减速
        /// </summary>
        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)//停止水平输入后减速
            {
                // 无输入时进行减速处理
                float decel = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;//判断是否在地面，选择对应减速度。
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, decel * Time.fixedDeltaTime);//减速
                //MoveTowards(current, target, maxDelta)：将current向target靠拢，但不超过maxDelta的步长。
            }
            else//有水平输入时加速
            {
                // 有输入时加速至最大速度
                _frameVelocity.x = Mathf.MoveTowards(//加速
                    _frameVelocity.x,
                    _frameInput.Move.x * _stats.MaxSpeed,//方向×最大速度大小
                    _stats.Acceleration * Time.fixedDeltaTime//加速度
                );
            }
        }
 
        #endregion
 
        #region Gravity
 
        /// <summary>
        /// 处理重力效果，包括着地时的下压力和空中重力
        /// </summary>
        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)//如果在地面且y轴速度为0或负 则施加额外力，辅助贴地
            {
                // 着地时施加额外下压力，帮助角色贴地
                _frameVelocity.y = _stats.GroundingForce;
            }
            else//处理在空中的速度
            {
                // 空中下落重力
                float gravity = _stats.FallAcceleration;
                // 提前松开跳跃时增加重力加速度
                if (_endedJumpEarly && _frameVelocity.y > 0)
                    gravity *= _stats.JumpEndEarlyGravityModifier;
 
                _frameVelocity.y = Mathf.MoveTowards(//下落速度计算，不超过最大下落速度
                    _frameVelocity.y,
                    -_stats.MaxFallSpeed,//最大下落速度
                    gravity * Time.fixedDeltaTime
                );
            }
        }
 
        #endregion
 
        /// <summary>
        /// 将计算后的速度应用到刚体
        /// </summary>
        private void ApplyMovement() => _rb.velocity = _frameVelocity;
 
#if UNITY_EDITOR
        private void OnValidate()
        {
            // 在编辑器中提醒绑定 Stats
            if (_stats == null)
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }
 
    // 玩家输入结构体
    public struct FrameInput
    {
        public bool JumpDown;   // 跳跃按下
        public bool JumpHeld;   // 跳跃持续
        public Vector2 Move;    // 水平与垂直移动输入
    }
 
    // 玩家控制接口
    public interface IPlayerController
    {
        event Action<bool, float> GroundedChanged;  // 地面状态事件
        event Action Jumped;                        // 跳跃事件
        Vector2 FrameInput { get; }                 // 当前移动输入(只读属性)
    }
}