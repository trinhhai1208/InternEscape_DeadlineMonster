using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển nhân vật chính: xử lý di chuyển 3 làn, vuốt trên Mobile, 
/// thay đổi Skin và các hiệu ứng tăng/giảm tốc độ.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  FIELDS — Inspector
    // ═══════════════════════════════════════════════════════════

    [Header("Movement")]
    [Tooltip("Tốc độ chạy cơ bản (units/giây)")]
    public float speed = 15f;

    [Tooltip("Tốc độ trượt ngang khi đổi lane")]
    public float laneSwitchSpeed = 15f;

    [Tooltip("Vị trí X của 3 làn đường")]
    public float[] lanes = { -5f, 0, 5f };

    [Header("Skin Models")]
    [Tooltip("Danh sách các model tiến hóa của nhân vật")]
    public GameObject[] skinModels;

    [Header("Audio (Tiếng Bước Chân)")]
    public AudioSource sfxSource;
    public AudioClip runningStepClip;
    public AudioClip walkingStepClip;

    [Header("Mobile Input")]
    [Tooltip("Ngưỡng vuốt tối thiểu để nhận diện (pixel)")]
    public float swipeThreshold = 50f;

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════

    private int _currentLane = 1;
    private Rigidbody _rb;
    private Animator _animator;
    private float _speedMultiplier = 1f;
    private bool _isSlowed = false;
    private Coroutine _activeBoostCoroutine;
    private int _currentSkinIndex = 0;
    private Vector2 _startTouchPosition;
    private bool _isSwiping = false;
    private float _lastSwipeTime = 0f;
    private float _swipeCooldown = 0.25f;
    private int _currentPathIndex = 0;

    /// <summary>
    /// Index skin hiện tại đang sử dụng.
    /// </summary>
    public int SkinIndex => _currentSkinIndex;

    // ═══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        ShowSkin(_currentSkinIndex);
        RefreshAnimator();

        if (sfxSource != null && runningStepClip != null)
        {
            sfxSource.clip = runningStepClip;
            sfxSource.loop = true;
            sfxSource.Play();
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleMobileInput();
        UpdateAnimations();
        UpdateAudioState();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying) return;
        if (GenMap.Instance == null || GenMap.Instance.splineSample == null || GenMap.Instance.splineSample.Count == 0) return;

        ApplyMovement();
    }

    // ═══════════════════════════════════════════════════════════
    //  INPUT HANDLING
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Xử lý điều khiển từ bàn phím (AD / Mũi tên).
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLeft();

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveRight();
    }

    /// <summary>
    /// Xử lý điều khiển vuốt chạm trên Mobile.
    /// </summary>
    private void HandleMobileInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _startTouchPosition = touch.position;
                _isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                if (_isSwiping)
                {
                    Vector2 endTouchPosition = touch.position;
                    float xDistance = endTouchPosition.x - _startTouchPosition.x;
                    float yDistance = endTouchPosition.y - _startTouchPosition.y;

                    if (Mathf.Abs(xDistance) > swipeThreshold && Mathf.Abs(xDistance) > Mathf.Abs(yDistance))
                    {
                        if (Time.time - _lastSwipeTime > _swipeCooldown)
                        {
                            if (xDistance < 0) MoveLeft();
                            else MoveRight();

                            _startTouchPosition = touch.position;
                            _lastSwipeTime = Time.time;
                        }
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  MOVEMENT LOGIC
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tính toán và áp dụng di chuyển vật lý dọc theo Spline.
    /// </summary>
    private void ApplyMovement()
    {
        float moveDelta = speed * _speedMultiplier * Time.fixedDeltaTime;

        // 1. Tận dụng mốc Path Index
        while (_currentPathIndex < GenMap.Instance.splineSample.Count - 1)
        {
            Vector3 nextPos = GenMap.Instance.splineSample[_currentPathIndex + 1].position;
            Vector3 flatNext = new Vector3(nextPos.x, 0, nextPos.z);
            Vector3 flatPlayer = new Vector3(_rb.position.x, 0, _rb.position.z);
            
            if (Vector3.Dot(flatNext - flatPlayer, GenMap.Instance.splineSample[_currentPathIndex].forward) <= 0 || 
                Vector3.Distance(flatPlayer, flatNext) < moveDelta)
            {
                _currentPathIndex++;
            }
            else break;
        }

        if (_currentPathIndex >= GenMap.Instance.splineSample.Count) 
            _currentPathIndex = GenMap.Instance.splineSample.Count - 1;

        PathSample sample = GenMap.Instance.splineSample[_currentPathIndex];

        // 2. Tịnh tiến tới trước
        Vector3 baseNewPos = _rb.position + sample.forward * moveDelta;

        // 3. Đổi làn mượt mà
        Vector3 offsetFromCenter = baseNewPos - sample.position;
        float currentLateral = Vector3.Dot(offsetFromCenter, sample.right);
        float targetLateral = lanes[_currentLane];
        float nextLateral = Mathf.Lerp(currentLateral, targetLateral, laneSwitchSpeed * Time.fixedDeltaTime);
        
        Vector3 finalPos = baseNewPos + sample.right * (nextLateral - currentLateral);
        finalPos.y = Mathf.Lerp(_rb.position.y, sample.position.y, 10f * Time.fixedDeltaTime);

        _rb.MovePosition(finalPos);

        // 4. Xoay theo địa hình
        Quaternion targetRotation = Quaternion.LookRotation(sample.forward, sample.up);
        _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
    }

    private void MoveLeft()
    {
        if (_currentLane > 0) _currentLane--;
    }

    private void MoveRight()
    {
        if (_currentLane < lanes.Length - 1) _currentLane++;
    }

    // ═══════════════════════════════════════════════════════════
    //  SKIN & ANIMATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật trang thái hoạt ảnh dựa trên tốc độ và hiệu ứng.
    /// </summary>
    private void UpdateAnimations()
    {
        if (_animator == null) return;

        if (GameManager.Instance != null && !GameManager.Instance.isGamePlaying)
            _animator.SetFloat(Constants.ANIM_SPEED, 0);
        else
            _animator.SetFloat(Constants.ANIM_SPEED, speed * _speedMultiplier);
    }

    /// <summary>
    /// Nâng cấp skin nhân vật khi thu thập Item.
    /// </summary>
    public void UpgradeSkin(float endgameSpeedMultiplier)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayItemSkinUp();

        _currentSkinIndex = Mathf.Min(_currentSkinIndex + 1, skinModels.Length - 1);
        ShowSkin(_currentSkinIndex);
        RefreshAnimator();

        GameManager.Instance.OnSkinUpCollected(_currentSkinIndex, endgameSpeedMultiplier);
    }

    private void ShowSkin(int index)
    {
        if (skinModels == null || skinModels.Length == 0) return;
        for (int i = 0; i < skinModels.Length; i++)
            if (skinModels[i] != null)
                skinModels[i].SetActive(i == index);
    }

    private void RefreshAnimator()
    {
        if (skinModels == null || _currentSkinIndex >= skinModels.Length) return;
        GameObject activeModel = skinModels[_currentSkinIndex];
        if (activeModel != null)
            _animator = activeModel.GetComponentInChildren<Animator>();
    }

    // ═══════════════════════════════════════════════════════════
    //  EFFECTS & AUDIO
    // ═══════════════════════════════════════════════════════════

    private void UpdateAudioState()
    {
        if (sfxSource == null) return;

        bool shouldPause = (GameManager.Instance != null && !GameManager.Instance.isGamePlaying) || Time.timeScale == 0;
        
        if (shouldPause)
        {
            if (sfxSource.isPlaying) sfxSource.Pause();
        }
        else
        {
            if (!sfxSource.isPlaying) sfxSource.UnPause();
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng tăng tốc từ cà phê.
    /// </summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayItemCoffee();
        if (_activeBoostCoroutine != null) StopCoroutine(_activeBoostCoroutine);
        _activeBoostCoroutine = StartCoroutine(BoostCoroutine(multiplier, duration));
    }

    /// <summary>
    /// Kích hoạt hiệu ứng làm chậm từ chướng ngại vật.
    /// </summary>
    public void ApplyObstacleSlow(float slowMultiplier = 0.5f, float duration = 2f)
    {
        if (AudioManager.Instance != null && !_isSlowed) AudioManager.Instance.PlayItemBug();
        if (_isSlowed) return;
        StartCoroutine(SlowCoroutine(slowMultiplier, duration));
    }

    /// <summary>
    /// Áp dụng điểm thưởng tốc độ cuối trận.
    /// </summary>
    public void ApplyEndgameSpeedBonus()
    {
        float mult = GameManager.Instance.GetEndgameSpeedMultiplier();
        if (mult > 1f)
        {
            speed *= mult;
        }
    }

    private IEnumerator BoostCoroutine(float multiplier, float duration)
    {
        _speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _speedMultiplier = 1f;
        _activeBoostCoroutine = null;
    }

    private IEnumerator SlowCoroutine(float slowMultiplier, float duration)
    {
        _isSlowed = true;
        _speedMultiplier = slowMultiplier;

        if (_animator != null)
            _animator.SetBool(Constants.ANIM_IS_SLOWED, true);

        if (sfxSource != null && walkingStepClip != null)
        {
            sfxSource.clip = walkingStepClip;
            sfxSource.Play();
        }

        yield return new WaitForSeconds(duration);

        _speedMultiplier = 1f;
        _isSlowed = false;

        if (_animator != null)
            _animator.SetBool(Constants.ANIM_IS_SLOWED, false);

        if (sfxSource != null && runningStepClip != null)
        {
            sfxSource.clip = runningStepClip;
            sfxSource.Play();
        }
    }
}