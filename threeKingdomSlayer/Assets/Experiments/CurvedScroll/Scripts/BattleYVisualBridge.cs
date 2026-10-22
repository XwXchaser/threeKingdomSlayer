using UnityEngine;

/// <summary>Runtime bridge for the Y visual layer. Does not move Battle player/enemy objects.</summary>
public sealed class BattleYVisualBridge : MonoBehaviour
{
    public YScrollSample route;
    public Transform visualRoot;
    public bool enabledInPlay = true;
    Vector3 basePosition; Quaternion baseRotation;
    void Awake(){if(!route)route=GetComponentInChildren<YScrollSample>(true);if(!visualRoot&&route)visualRoot=route.transform;if(visualRoot){basePosition=visualRoot.position;baseRotation=visualRoot.rotation;}}
    public void Apply(float progress,bool right)
    {
        if(!visualRoot||!route)return;
        float heading;var pose=YScrollSample.Evaluate(progress*route.Length,route.turnAngle,right,out heading);
        visualRoot.SetPositionAndRotation(basePosition-pose,Quaternion.Euler(0,-heading,0)*baseRotation);
    }
    public void Restore(){if(visualRoot)visualRoot.SetPositionAndRotation(basePosition,baseRotation);}
}
