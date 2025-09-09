using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyTurnHandler : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterMove characterMove;
    [SerializeField] private Transform mCamera;
    [SerializeField] private Transform body;
    [SerializeField] private float turnTime = 1f;
    [SerializeField] private float angleOfRotation = 45f;

    public float currentAngle { get; private set; }
    public bool momentaryTurn = false;
    private IEnumerator _turnCoroutine;
    private float _angle; // body rotation angle
    public float Angle
    {
        get => _angle;
        set
        {
            _angle = value;

            // calculating which way to turn
            if (_angle > 90 + angleOfRotation | _angle < 90 - angleOfRotation)
            {
                if (_turnCoroutine != null) StopCoroutine(_turnCoroutine);

                var rotation = body.rotation;
                rotation = _angle < 90 + angleOfRotation ? Quaternion.Euler(0, rotation.eulerAngles.y + ((90 - angleOfRotation) - value), 0) : Quaternion.Euler(0, rotation.eulerAngles.y + ((90 + angleOfRotation) - value), 0);
                body.rotation = rotation;

                _turnCoroutine = BodyTurn();
                // start turning Coroutine
                StartCoroutine(_turnCoroutine);
            }
        }
    }


    private void Update()
    {
        if (momentaryTurn)
        {
            body.rotation = Quaternion.Euler(0, mCamera.rotation.eulerAngles.y, 0);
        }
        else
        {
            // body rotation calculation
            Angle = Vector3.Angle(body.right, Quaternion.Euler(0, mCamera.rotation.eulerAngles.y, 0) * Vector3.forward);
        }
    }

    IEnumerator BodyTurn()
    {
        if (characterMove.currentState == characterMove.standState)
        {
            animator.Play("Turn_" + (_angle > 90 + angleOfRotation ? "Left" : "Right"), 2); // start animation of body rotation
        }

        // body rotation
        float t = 0;

        Quaternion startRot = body.rotation;

        while (t < turnTime)
        {
            t += Time.deltaTime;

            body.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(0, mCamera.rotation.eulerAngles.y, 0), t / turnTime);

            yield return null;
        }

        _turnCoroutine = null;
    }
}
