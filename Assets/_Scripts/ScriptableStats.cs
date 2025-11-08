using UnityEngine;
 
namespace TarodevController
{
    [CreateAssetMenu]
    public class ScriptableStats : ScriptableObject
    {
        [Header("LAYERS")]
        // 图层
        [Tooltip("Set this to the layer your player is on")]
        // 将此设置为玩家所在的图层
        public LayerMask PlayerLayer;
 
        [Header("INPUT")]
        // 输入
        [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keyboard parity.")]
        // 将所有输入四舍五入为整数。防止手柄缓慢移动。建议开启以确保手柄和键盘输入效果一致。
        public bool SnapInput = true;
 
        [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers")]
        // 攀爬梯子或翻越台阶前需要的最小垂直输入，避免手柄漂移导致的意外攀爬
        [Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;
 
        [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers")]
        // 识别左右移动前需要的最小水平输入，避免手柄漂移
        [Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;
 
        [Header("MOVEMENT")]
        // 移动
        [Tooltip("The top horizontal movement speed")]
        // 最高水平移动速度
        public float MaxSpeed = 14;
 
        [Tooltip("The player's capacity to gain horizontal speed")]
        // 玩家水平速度增长的能力
        public float Acceleration = 120;
 
        [Tooltip("The pace at which the player comes to a stop")]
        // 玩家停止输入时地面上的减速速度
        public float GroundDeceleration = 60;
 
        [Tooltip("Deceleration in air only after stopping input mid-air")]
        // 空中停止输入后仅在空中生效的减速速度
        public float AirDeceleration = 30;
 
        [Tooltip("A constant downward force applied while grounded. Helps on slopes")]
        // 落地时向下的恒定作用力，帮助角色在斜坡上稳定
        [Range(0f, -10f)]
        public float GroundingForce = -1.5f;
 
        [Tooltip("The detection distance for grounding and roof detection")]
        // 检测地面和头顶碰撞的射线长度
        [Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;
 
        [Header("JUMP")]
        // 跳跃
        [Tooltip("The immediate velocity applied when jumping")]
        // 跳跃时立即施加的初始向上速度
        public float JumpPower = 36;
 
        [Tooltip("The maximum vertical movement speed")]
        // 允许的最大自由落体速度
        public float MaxFallSpeed = 40;
 
        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        // 空中重力加速度程度
        public float FallAcceleration = 110;
 
        [Tooltip("The gravity multiplier added when jump is released early")]
        // 提前释放跳跃键时施加的额外重力倍数
        public float JumpEndEarlyGravityModifier = 3;
 
        [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
        // “科约特时间”持续时长，在离开平台后仍可短暂跳跃
        public float CoyoteTime = .15f;
 
        [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
        // 可缓存的跳跃输入时长，允许在即将着地前按下跳跃键
        public float JumpBuffer = .2f;
    }
}