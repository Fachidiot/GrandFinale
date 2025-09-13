using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilController : MonoBehaviour
{
    public float timeScale;
    public EventsCenter eventsCenter;
    public CameraController cameraController;
    public WeaponController weaponController;
    public RecoilParametersModel recoilParametersModel;
    public Transform weaponRotationRecoilPivot;
    public Transform weaponPositionRecoilPivot;

    public Vector3 lastPosition;
    public Quaternion lastRotation;

    private void OnEnable()
    {
        eventsCenter.OnWeaponChange += weaponChangeCheck;
        weaponController.OnShoot += RecoilStarter;
    }
    private void OnDisable()
    {
        eventsCenter.OnWeaponChange -= weaponChangeCheck;
        weaponController.OnShoot -= RecoilStarter;
    }

    private void Start()
    {
        recoilParametersModel = weaponController.GETCurrentWeapon.RecoilParameters;
    }

    private void Update()
    {
        Time.timeScale = timeScale;
    }

    void weaponChangeCheck(bool changed)
    {
        if (!changed) recoilParametersModel = weaponController.GETCurrentWeapon.RecoilParameters;
    }

    void RecoilStarter()
    {
        StopAllCoroutines();

        StartCoroutine(ApplyCameraRecoil(weaponController.GETCurrentWeapon.RecoilParameters.cameraRecoilAxes));

        StartCoroutine(ApplyPositionRecoil(weaponRotationRecoilPivot, weaponController.GETCurrentWeapon.RecoilParameters.weaponRotationRecoilAxes, true));

        StartCoroutine(ApplyPositionRecoil(weaponPositionRecoilPivot, weaponController.GETCurrentWeapon.RecoilParameters.weaponPositionRecoilAxes, false));


    }

    IEnumerator ApplyCameraRecoil(RecoilParametersModel.RecoilAxis[] recoilAxes)
    {
        if (recoilAxes.Length == 0) yield break;

        Vector3 finalState = GetFinalState(recoilAxes);

        float t = 0;
        while (t < 1)
        {
            var verticalRecoil = finalState.x * recoilAxes[0].axisValueCurve.Evaluate(t / 1);
            var horizontalRecoil = recoilAxes.Length > 1 ? finalState.y * recoilAxes[1].axisValueCurve.Evaluate(t / 1) : 0;

            cameraController.SetCameraRotation(verticalRecoil, horizontalRecoil);

            t += Time.deltaTime;

            yield return null;
        }
    }

    IEnumerator ApplyPositionRecoil(Transform pivot, RecoilParametersModel.RecoilAxis[] recoilAxes, bool recoilRotate)
    {
        var startState = recoilRotate ? GetStartState(pivot.localRotation.eulerAngles, recoilAxes) : GetStartState(pivot.localPosition, recoilAxes);

        var finalPosition = GetFinalState(recoilAxes);

        var pivotState = recoilRotate ? pivot.localPosition : pivot.localRotation.eulerAngles;

        var recoilValue = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            recoilValue = new Vector3(
                finalPosition.x * recoilAxes[0].axisValueCurve.Evaluate(t / 1),
                finalPosition.y * (recoilAxes.Length > 1 ? recoilAxes[1].axisValueCurve.Evaluate(t / 1) : 0),
                finalPosition.z * (recoilAxes.Length > 2 ? recoilAxes[2].axisValueCurve.Evaluate(t / 1) : 0)
            );

            var currentState = Vector3.zero;
            var currentRotation = Quaternion.identity;

            if (recoilRotate)
            {
                currentRotation = GetCurrentRotationState(lastRotation, recoilValue, recoilAxes, t);
                pivot.localRotation = currentRotation;
                lastRotation = pivot.localRotation;
            }
            else
            {
                currentState = GetCurrentPositionState(lastPosition, recoilValue, recoilAxes, t);
                pivot.localPosition = currentState;
                lastPosition = currentState;
            }


            t += Time.deltaTime;
            yield return null;
        }
    }

    Vector3 GetStartState(Vector3 currentPivotPosition, RecoilParametersModel.RecoilAxis[] recoilAxes)
    {
        var xValue = recoilAxes.Length > 0 && recoilAxes[0].smoothReturnTime > 0 ? currentPivotPosition.x : 0;
        var yValue = recoilAxes.Length > 1 && recoilAxes[1].smoothReturnTime > 0 ? currentPivotPosition.y : 0;
        var zValue = recoilAxes.Length > 2 && recoilAxes[2].smoothReturnTime > 0 ? currentPivotPosition.z : 0;

        return new Vector3(xValue, yValue, zValue);
    }

    Vector3 GetFinalState(RecoilParametersModel.RecoilAxis[] recoilAxes)
    {
        float xValue = 0;
        if (recoilAxes.Length > 0)
        {
            xValue = GetFinalAxisValue(recoilAxes[0]);
        }
        float yValue = 0;
        if (recoilAxes.Length > 1)
        {
            yValue = GetFinalAxisValue(recoilAxes[1]);
        }
        float zValue = 0;
        if (recoilAxes.Length > 2)
        {
            zValue = GetFinalAxisValue(recoilAxes[2]);
        }

        return new Vector3(xValue, yValue, zValue);
    }

    float GetFinalAxisValue(RecoilParametersModel.RecoilAxis recoilAxis)
    {
        float value = 0;

        float force = recoilParametersModel.recoilForce * recoilAxis.percentageOfRecoilValue;
        if (recoilAxis.randomValue)
        {
            value = UnityEngine.Random.Range(recoilAxis.canBeNegative ? force * -1 : 0, force);
        }
        else
        {
            value = force;
        }

        return value;
    }

    Vector3 GetCurrentPositionState(Vector3 startVector, Vector3 currentRecoilVector, RecoilParametersModel.RecoilAxis[] recoilAxis, float recoiledTime)
    {
        float xValue = 0;
        if (recoilAxis.Length > 0)
        {
            xValue = recoilAxis[0].smoothReturnTime > 0 && (recoiledTime / recoilAxis[0].smoothReturnTime) < 1 ? Mathf.Lerp(startVector.x, currentRecoilVector.x, recoiledTime / recoilAxis[0].smoothReturnTime) : currentRecoilVector.x;
        }

        float yValue = 0;
        if (recoilAxis.Length > 1)
        {
            yValue = recoilAxis[1].smoothReturnTime > 0 && (recoiledTime / recoilAxis[1].smoothReturnTime) < 1 ? Mathf.Lerp(startVector.y, currentRecoilVector.y, recoiledTime / recoilAxis[1].smoothReturnTime) : currentRecoilVector.y;
        }

        float zValue = 0;
        if (recoilAxis.Length > 2)
        {
            zValue = recoilAxis[2].smoothReturnTime > 0 && (recoiledTime / recoilAxis[2].smoothReturnTime) < 1 ? Mathf.Lerp(startVector.z, currentRecoilVector.z, recoiledTime / recoilAxis[2].smoothReturnTime) : currentRecoilVector.z;
        }

        return new Vector3(xValue, yValue, zValue);
    }

    Quaternion GetCurrentRotationState(Quaternion startRot, Vector3 currentRecoilVector, RecoilParametersModel.RecoilAxis[] recoilAxis, float recoiledTime)
    {
        Quaternion lerpRot = Quaternion.identity;

        float xValue = 0;
        float yValue = 0;
        float zValue = 0;

        if (recoilAxis.Length > 0)
        {
            float startX = startRot.eulerAngles.x;
            startX += startRot.eulerAngles.x < -180 ? +360f : 0;
            startX += startRot.eulerAngles.x > 180 ? -360f : 0;

            xValue = recoilAxis[0].smoothReturnTime > 0 && (recoiledTime / recoilAxis[0].smoothReturnTime) < 1 ? Mathf.Lerp(startX, currentRecoilVector.x, recoiledTime / recoilAxis[0].smoothReturnTime) : currentRecoilVector.x;
        }

        if (recoilAxis.Length > 1)
        {
            float startY = startRot.eulerAngles.y;
            startY += startRot.eulerAngles.y < -180 ? +360f : 0;
            startY += startRot.eulerAngles.y > 180 ? -360f : 0;

            yValue = recoilAxis[1].smoothReturnTime > 0 && (recoiledTime / recoilAxis[1].smoothReturnTime) < 1 ? Mathf.Lerp(startY, currentRecoilVector.y, recoiledTime / recoilAxis[1].smoothReturnTime) : currentRecoilVector.y;
        }

        if (recoilAxis.Length > 2)
        {
            float startZ = startRot.eulerAngles.z;
            startZ += startRot.eulerAngles.z < -180 ? +360f : 0;
            startZ += startRot.eulerAngles.z > 180 ? -360f : 0;

            zValue = recoilAxis[2].smoothReturnTime > 0 && (recoiledTime / recoilAxis[2].smoothReturnTime) < 1 ? Mathf.Lerp(startZ, currentRecoilVector.z, recoiledTime / recoilAxis[2].smoothReturnTime) : currentRecoilVector.z;
        }

        var q = Quaternion.Euler(xValue, yValue, zValue);

        return q;
    }
}
