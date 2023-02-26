using System;
using Enums;
using Extensions;
using Interfaces;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(IInputProvider))]
public class PlatformMovement : MonoBehaviour
{
    private Rigidbody2D _rigidbody;
    private IInputProvider _inputProvider;
    private ICheck _groundCheck;
    // Animator
    private Animator _animator;
    private float _inputX;
    private int _currentState;
    private bool _jumpTriggered;
    private float _lockedTill;

    [Header("Movement Configuration")]
    [SerializeField]
    private float walkSpeed;

    [SerializeField]
    private float jumpForce;

    [SerializeField]
    private GameObject groundCheckObject;

    // Animator
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Walk = Animator.StringToHash("Walk");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int Fall = Animator.StringToHash("Fall");

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _inputProvider = GetComponent<IInputProvider>();
        _groundCheck = groundCheckObject.GetComponent<ICheck>();
        // Animator
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyJump();
        ApplyFall();
    }

    private void Update()
    {
        CaptureHorizontalInput();
        ApplyWalkingDirection();

        var state = GetState();

        if (state == _currentState) return;
        _animator.CrossFade(state, 0.35f, 0);
        _currentState = state;
    }


    private void ApplyJump()
    {
        if (IsGrounded() && _inputProvider.GetActionPressed(InputAction.Jump))
        {
            _rigidbody.SetVelocity(Axis.Y, jumpForce);
            _animator.SetTrigger(Jump);
            _animator.CrossFade(Jump, 0, 0);
            _jumpTriggered = true;
            _currentState = LockState(Jump, 0.2f); // lock state for 0.2 seconds
        }
        else if (Time.time < _lockedTill && _currentState == Jump)
        {
            _jumpTriggered = false;
        }
        else
        {
            _jumpTriggered = false;
            _currentState = GetState(); // unlock the state and update the current state
        }
    }


    private void ApplyFall()
    {
        if (!_groundCheck.Check() && _rigidbody.velocity.y < 0)
        {
            // Set FallSpeed parameter based on falling velocity
            _animator.SetFloat("FallSpeed", Mathf.Abs(_rigidbody.velocity.y));

            if (_currentState != Fall)
            {
                // Transition to Fall state with a crossfade if not already in Fall state
                _animator.CrossFade(Fall, 0.1f, 0);
                _currentState = LockState(Fall, 0.2f); // lock state for 0.2 seconds
            }
        }
        else
        {
            _animator.SetFloat("FallSpeed", 0);

            if (_currentState == Fall)
            {
                // Transition to Idle state with a crossfade if in Fall state
                _animator.CrossFade(Idle, 0.1f, 0);
                _currentState = LockState(Idle, 0.2f); // lock state for 0.2 seconds
            }
        }
    }


    private bool IsGrounded()
    {
        return _groundCheck.Check();
    }

    private void ApplyHorizontalMovement()
    {
        _rigidbody.SetVelocity(Axis.X, _inputX * walkSpeed);

        if (_inputX != 0)
        {
            _animator.SetBool(Walk, true);
            _animator.CrossFade(Walk, 0, 0);
        }
        else
        {
            _animator.SetBool(Walk, false);
            _animator.CrossFade(Idle, 0, 0);
        }
    }

    private void ApplyWalkingDirection()
    {
        if (_inputX != 0)
        {
            transform.localScale = new Vector3(MathF.Sign(_inputX), 1, 1);
        }

    }

    private void CaptureHorizontalInput()
    {
        _inputX = _inputProvider.GetAxis(Axis.X);
    }

    private int GetState()
    {
        if (Time.time < _lockedTill && _currentState == Fall) return _currentState;

        // Priorities
        if (_jumpTriggered) return Jump; // prioritize jump state over idle state

        if (!_groundCheck.Check())
        {
            return Fall; // prioritize fall state over idle state
        }
        else if (_rigidbody.velocity.y > 0)
        {
            return Jump;
        }
        else
        {
            return _inputX == 0 ? Idle : Walk;
        }
    }

    private int LockState(int s, float t)
    {
        _lockedTill = Time.time + t;
        return s;
    }

}