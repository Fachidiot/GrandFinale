using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltAnimation : MonoBehaviour
{
    public Transform boltTransform;
    public float zOffset;
    Vector3 startPosition;
    IEnumerator animEnum;

    private void Start()
    {
        startPosition = boltTransform.localPosition;
    }

    public void StartAnim(float _time)
    {
        if (animEnum != null) StopCoroutine(animEnum);
        animEnum = AnimationUpdate(_time);
        StartCoroutine(animEnum);
    }

    IEnumerator AnimationUpdate(float _time)
    {
        Vector3 finalPos = new Vector3(startPosition.x, startPosition.y, startPosition.z + zOffset);

        float t = 0;
        while (t < _time)
        {
            t += Time.deltaTime;
            boltTransform.localPosition = Vector3.Lerp(startPosition, finalPos, t / _time);
            yield return null;
        }

        float returnT = 0;
        while (returnT < 0.2f)
        {
            returnT += Time.deltaTime;
            boltTransform.localPosition = Vector3.Lerp(finalPos, startPosition, returnT / 0.2f);
            yield return null;
        }
        yield break;
    }
}
