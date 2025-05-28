using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterIKSetup : MonoBehaviour
{
    public TwoBoneIKConstraint leftHandIK;
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftFootIK;  
    public TwoBoneIKConstraint rightFootIK;

    private void OnDrawGizmos()
{
    if (leftHandIK != null && leftHandIK.data.target != null && leftHandIK.data.hint != null)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(leftHandIK.transform.position, leftHandIK.data.target.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(leftHandIK.data.target.position, leftHandIK.data.hint.position);
        Gizmos.DrawSphere(leftHandIK.data.target.position, 0.05f);
        Gizmos.DrawSphere(leftHandIK.data.hint.position, 0.05f);
    }

    if (rightHandIK != null && rightHandIK.data.target != null && rightHandIK.data.hint != null)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(rightHandIK.transform.position, rightHandIK.data.target.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rightHandIK.data.target.position, rightHandIK.data.hint.position);
        Gizmos.DrawSphere(rightHandIK.data.target.position, 0.05f);
        Gizmos.DrawSphere(rightHandIK.data.hint.position, 0.05f);
    }

    // Tương tự cho chân
    if (leftFootIK != null && leftFootIK.data.target != null && leftFootIK.data.hint != null)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(leftFootIK.transform.position, leftFootIK.data.target.position);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(leftFootIK.data.target.position, leftFootIK.data.hint.position);
        Gizmos.DrawSphere(leftFootIK.data.target.position, 0.05f);
        Gizmos.DrawSphere(leftFootIK.data.hint.position, 0.05f);
    }

    if (rightFootIK != null && rightFootIK.data.target != null && rightFootIK.data.hint != null)
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(rightFootIK.transform.position, rightFootIK.data.target.position);
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(rightFootIK.data.target.position, rightFootIK.data.hint.position);
        Gizmos.DrawSphere(rightFootIK.data.target.position, 0.05f);
        Gizmos.DrawSphere(rightFootIK.data.hint.position, 0.05f);
    }
}
    
    public void SitInCar(GameObject car)
    {
        var lhTarget = car.transform.Find("LeftHandTarget");
        var rhTarget = car.transform.Find("RightHandTarget");
        var lhHint = car.transform.Find("ElbowHintLeft");
        var rhHint = car.transform.Find("ElbowHintRight");
        
        var lfTarget = car.transform.Find("LeftFootPlacement");
        var rfTarget = car.transform.Find("RightFootPlacement");
        var lfHint = car.transform.Find("LeftKneeHint");
        var rfHint = car.transform.Find("RightKneeHint");
        
        var seat = car.transform.Find("SeatPosition");
        
        leftHandIK.data.target = lhTarget;
        leftHandIK.data.hint = lhHint;
        
        Debug.Log($"Left Hand Target: {lhTarget}, Hint: {lhHint}"); 

        rightHandIK.data.target = rhTarget;
        rightHandIK.data.hint = rhHint;
        
        Debug.Log($"Right Hand Target: {rhTarget}, Hint: {rhHint}");
        
        leftFootIK.data.target = lfTarget;
        leftFootIK.data.hint = lfHint;
        
        Debug.Log($"Left Foot Target: {lfTarget}, Hint: {lfHint}");
        
        rightFootIK.data.target = rfTarget;
        rightFootIK.data.hint = rfHint;
        
        leftHandIK.weight = 1f;
        rightHandIK.weight = 1f;
        leftFootIK.weight = 1f;
        rightFootIK.weight = 1f;
        
        Debug.Log($"Right Foot Target: {rfTarget}, Hint: {rfHint}");
        
        transform.SetPositionAndRotation(seat.position, seat.rotation);
    }
}
