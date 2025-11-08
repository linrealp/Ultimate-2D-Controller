using TarodevController;
using UnityEngine;
 
namespace TarodevController
{
    /// <summary>
    /// 非常基础的角色动画示例，演示如何根据角色状态驱动动画、翻转、倾斜及粒子效果。
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField]
        private Animator _anim;                        // 引用 Animator 组件，用于控制动画状态机
 
        [SerializeField] private SpriteRenderer _sprite; // 精灵渲染器，用于左右翻转
 
        [Header("Settings")] 
        [SerializeField, Range(1f, 3f)]
        private float _maxIdleSpeed = 2;               // 空闲时动画速度的最大倍率
 
        [SerializeField] private float _maxTilt = 5;   // 最大倾斜角度（度数）
        [SerializeField] private float _tiltSpeed = 20; // 倾斜插值速度
 
        [Header("Particles")] 
        [SerializeField] private ParticleSystem _jumpParticles;   // 跳跃时的粒子特效
        [SerializeField] private ParticleSystem _launchParticles; // 起跳发射效果
        [SerializeField] private ParticleSystem _moveParticles;   // 移动持续粒子特效
        [SerializeField] private ParticleSystem _landParticles;   // 落地粒子特效
 
        [Header("Audio Clips")] 
        [SerializeField]
        private AudioClip[] _footsteps;               // 脚步音频列表，用于随机播放脚步声
 
        private AudioSource _source;                   // 音频源组件
        private IPlayerController _player;             // 角色控制接口
        private bool _grounded;                        // 当前是否在地面
        private ParticleSystem.MinMaxGradient _currentGradient; // 粒子颜色渐变
 
        private void Awake()
        {
            // 缓存组件引用
            _source = GetComponent<AudioSource>();// 音频源组件
            _player = GetComponentInParent<IPlayerController>();// 角色控制接口
        }
 
        private void OnEnable()
        {
            // 订阅跳跃与落地事件
            _player.Jumped += OnJumped;//跳跃事件
            _player.GroundedChanged += OnGroundedChanged;//着陆状态改变
 
            // 启动移动粒子
            _moveParticles.Play();
        }
 
        private void OnDisable()
        {
            // 取消订阅，防止内存泄漏
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;
 
            // 停止移动粒子
            _moveParticles.Stop();
        }
 
        private void Update()
        {
            if (_player == null) return;
 
            // 检测地面下方颜色，用于粒子染色
            DetectGroundColor();
 
            // 根据水平输入翻转精灵
            HandleSpriteFlip();
 
            // 根据输入强度调节空闲动画速度及移动粒子大小
            HandleIdleSpeed();
 
            // 根据地面状态和平移输入倾斜角色
            HandleCharacterTilt();
        }
 
        /// <summary>
        /// 根据输入方向翻转精灵
        /// </summary>
        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0)
                _sprite.flipX = _player.FrameInput.x < 0;
        }
 
        /// <summary>
        /// 调整空闲动画速度和移动粒子缩放
        /// </summary>
        private void HandleIdleSpeed()
        {
            float inputStrength = Mathf.Abs(_player.FrameInput.x);//(开启SnapInput时为1)
            // 空闲时速率在 1 到 _maxIdleSpeed 之间根据inputStrength插值
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            // 移动粒子根据输入强度缩放
            _moveParticles.transform.localScale = Vector3.MoveTowards(
                _moveParticles.transform.localScale,
                Vector3.one * inputStrength,
                2 * Time.deltaTime
            );
        }
 
        /// <summary>
        /// 角色倾斜效果，跑动时身体向前倾
        /// </summary>
        private void HandleCharacterTilt()
        {
            // 如果在地面，根据输入角度生成旋转，否则回正
            Quaternion target = _grounded
                ? Quaternion.Euler(0, 0, _maxTilt * _player.FrameInput.x)
                : Quaternion.identity;
            // 平滑插值当前 transform.up(决定了当前动画对象的朝向)
            _anim.transform.up = Vector3.RotateTowards(
                _anim.transform.up,
                target * Vector2.up,
                _tiltSpeed * Time.deltaTime,
                0f
            );
        }
 
        /// <summary>
        /// 跳跃时触发动画与粒子效果
        /// </summary>
        private void OnJumped()//跳跃事件
        {
            // 播放跳跃动画
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);
 
            if (_grounded) // 仅在非土狼跳时播放粒子
            {
                //设置粒子颜色
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                //播放粒子
                _jumpParticles.Play();
                _launchParticles.Play();
            }
        }
 
        /// <summary>
        /// 落地状态改变时触发动画、声音、粒子
        /// </summary>
        private void OnGroundedChanged(bool grounded, float impact)
        {
            //获取当前是否在地面
            _grounded = grounded;
 
            if (grounded)
            {
                DetectGroundColor();              // 更新粒子颜色
                SetColor(_landParticles);         // 应用颜色梯度
 
                _anim.SetTrigger(GroundedKey);    // 播放落地动画
                // 随机脚步声
                _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                _moveParticles.Play();            // 恢复移动粒子
 
                // 根据冲击强度缩放落地粒子
                // Mathf.InverseLerp 反向插值 返回的是一个 0到1之间的比例值，表示 value 在范围 [a, b] 中的位置。
                float scale = Mathf.InverseLerp(0, 40, impact);//落地的时候会impact传过来y轴速度，不在地面的时候传过来0
                _landParticles.transform.localScale = Vector3.one * scale;//缩放粒子
                _landParticles.Play();            // 播放落地粒子
            }
            else
            {
                // 离地时停止移动粒子
                _moveParticles.Stop();
            }
        }
 
        /// <summary>
        /// 向下射线检测地面颜色，用于粒子染色
        /// </summary>
        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);//射线检测，以当前位置为起点，向下检测2的距离
            if (!hit || hit.collider.isTrigger) return;//如果没有碰到东西或者碰到的东西是触发器，则不处理
 
            // 如果碰撞物带有 SpriteRenderer，则获取其颜色
            if (hit.transform.TryGetComponent(out SpriteRenderer r))
            {
                Color color = r.color;
                // 生成渐变色用于粒子
                _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);//在原颜色的基础上有一定的亮度变化
                SetColor(_moveParticles);
            }
        }
 
        /// <summary>
        /// 设置粒子系统的起始颜色
        /// </summary>
        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;//获取粒子系统的主模块
            main.startColor = _currentGradient;//设置粒子颜色
        }
 
        // Animator 参数的哈希值，优化性能
        //StringToHash 方法将 Animator 参数名（字符串）转换成一个整数哈希值。
        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
    }
}
 