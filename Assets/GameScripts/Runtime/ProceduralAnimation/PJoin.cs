using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PJoin : MonoBehaviour
{
    public float leftRad;
    public float rightRad;
    public float radius;

    public float SlotRad;
    public Vector3 SlotForward = Vector3.forward;
    public PJoin Parent;

    public float distanceConstraintMin;
    public float distanceConstraintMax;
    public float distanceConstraint;

    public bool isFixedRotation = true;
    public bool isFixedDistance = true;
    static Vector3 RotateUnitVector(Vector3 direction,float radians)
    {
        if (!Mathf.Approximately(direction.magnitude, 1f))
        {
            direction = direction.normalized;
        }
    
        float cosA = Mathf.Cos(radians);
        float sinA = Mathf.Sin(radians);

        float x = direction.x * cosA + direction.z * sinA;
        float z = -direction.x * sinA + direction.z * cosA;
        return new Vector3(x, 0, z);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        if (Parent != null)
        {
            transform.forward = (Parent.transform.position - transform.position).normalized;    
        }
        
        //当前方向
        Vector3 dest = transform.position;
        dest.x += transform.forward.x * radius;
        dest.z += transform.forward.z * radius;
        Gizmos.DrawLine(transform.position,dest);
        
        // float forwardRad = Mathf.Atan2(transform.forward.z, transform.forward.x);
        
        //左边
        Vector3 leftForward = RotateUnitVector(transform.forward,leftRad);
        dest.x = transform.position.x + leftForward.x * radius;
        dest.z = transform.position.z + leftForward.z * radius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(dest, new(.1f,.1f,.1f));
        //右边
        Vector3 rightForward = RotateUnitVector(transform.forward,rightRad);
        dest.x = transform.position.x + rightForward.x * radius;
        dest.z = transform.position.z + rightForward.z * radius;
        Gizmos.color = Color.green;
        Gizmos.DrawCube(dest,new(.1f,.1f,.1f));
        // Debug.Log("forwardRad: "+forwardRad);
        //插槽
        // Vector3 slotVec = RotateUnitVector(SlotForward, SlotRad);
        //to在from左边 为负 
        // float slotToLeftRad = Vector3.SignedAngle(leftForward,slotVec,Vector3.up) * Mathf.Deg2Rad;
        // float slotToRightDeg = Vector3.SignedAngle(rightForward,slotVec,Vector3.up) * Mathf.Deg2Rad;
        //
        // if (Mathf.Abs(slotToLeftRad) <= 0.1f)
        // {
        //     SlotForward = RotateUnitVector(SlotForward,-0.1f);
        // }
        //
        // if (Mathf.Abs(slotToRightDeg) <= 0.1f)
        // {
        //     SlotForward = RotateUnitVector(SlotForward,0.1f);
        // }
        Gizmos.color = Color.white;
        Vector3 SlotForwardDest = new Vector3();
        SlotForwardDest.x = transform.position.x + SlotForward.x * radius;
        SlotForwardDest.z = transform.position.z + SlotForward.z * radius;
        Gizmos.DrawLine(transform.position,SlotForwardDest);
        
        Vector3 curSlotForward = RotateUnitVector(SlotForward, SlotRad);
        dest.x = transform.position.x + curSlotForward.x * radius;
        dest.z = transform.position.z + curSlotForward.z * radius;
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(dest,new(.1f,.1f,.1f));
    }

    public bool PermitRotate(Vector3 childRequestPosition)
    {
        Vector3 requestChildVec = (childRequestPosition - transform.position).normalized;
        Vector3 leftForward = RotateUnitVector(transform.forward,leftRad);
        Vector3 rightForward = RotateUnitVector(transform.forward,rightRad);
        
        float slotToLeftRad = Vector3.SignedAngle(leftForward,requestChildVec,Vector3.up) * Mathf.Deg2Rad;
        float slotToRightRad = Vector3.SignedAngle(rightForward,requestChildVec,Vector3.up) * Mathf.Deg2Rad;
        if (slotToLeftRad * slotToRightRad < 0)
        {
            return true;
        }
        
        //如果是固定节点 并且情况超出限制角度 直接拒绝
        if (Parent == null && isFixedRotation)
        { 
            return false;
        }
        
        //尝试旋转自身
        float tmpLeft = Vector3.Dot(leftForward, requestChildVec);
        float tmpRight = Vector3.Dot(rightForward, requestChildVec);
        float rotateRad = slotToLeftRad * -1.1f;
        if (tmpRight > tmpLeft)
        {
            rotateRad = slotToRightRad * -1.1f;    
        }
        Vector3 slotTryRotation = RotateUnitVector(SlotForward,rotateRad);
        //如果有父节点 询问父节点是否允许
        if (Parent)
        {
            bool isPermitted = Parent.PermitRotate(transform.position);
            if (isPermitted)
            {
                SlotForward = slotTryRotation;
                return true;
            }
            return false;
        }
        //当前节点是否为 固定节点
        if (isFixedRotation)
        {
            return false;
        }
        SlotForward = slotTryRotation;
        return true;
    }

    public void ForwardMove()
    {
        Vector3 nextPosition = transform.forward * Time.deltaTime * (1/60);
        if (Parent != null)
        {
            bool isPermitted = Parent.PermitRotate(nextPosition);
            if (isPermitted)
            {
                transform.position = nextPosition;
            }
        }
        Calculate();
    }
    public void Calculate()
    {
        // if (Parent != null)
        // {
        //     Parent.PermitRotate();
        //     // Vector3 parentSlotForward = RotateUnitVector(Parent.SlotForward, Parent.SlotRad);
        //     // Vector3 newPos = new();
        //     // newPos.x = Parent.transform.position.x + parentSlotForward.x * (distanceConstraint);
        //     // newPos.z = Parent.transform.position.z + parentSlotForward.z * (distanceConstraint);
        //     // transform.position = newPos;
        //     // transform.forward = (Parent.transform.position - transform.position).normalized;    
        // }
        //当前方向
        Vector3 dest = transform.position;
        dest.x += transform.forward.x * radius;
        dest.z += transform.forward.z * radius;
        //左边
        Vector3 leftForward = RotateUnitVector(transform.forward,leftRad);
        dest.x = transform.position.x + leftForward.x * radius;
        dest.z = transform.position.z + leftForward.z * radius;
        //右边
        Vector3 rightForward = RotateUnitVector(transform.forward,rightRad);
        dest.x = transform.position.x + rightForward.x * radius;
        dest.z = transform.position.z + rightForward.z * radius;
        //插槽
        Vector3 slotVec = RotateUnitVector(SlotForward, SlotRad);
        // to在from左边 为负
        float slotToLeftRad = Vector3.SignedAngle(leftForward,slotVec,Vector3.up) * Mathf.Deg2Rad;
        float slotToRightRad = Vector3.SignedAngle(rightForward,slotVec,Vector3.up) * Mathf.Deg2Rad;
        //如果slotTOLeftRad和slotToRightDeg是同一符号的话 说明当前slot已经不在Left和Right中间
        if (slotToLeftRad * slotToRightRad > 0)
        {
            float tmpLeft = Vector3.Dot(leftForward, slotVec);
            float tmpRight = Vector3.Dot(rightForward, slotVec);
            float rotateRad = slotToLeftRad * -1.1f;
            if (tmpRight > tmpLeft)
            {
                rotateRad = slotToRightRad * -1.1f;    
            }
            // rotateRad *= -1.0f;
            Debug.Log("tmpLeft = "+tmpLeft+" tmpRight "+tmpRight+" rotateRad "+rotateRad);
            Debug.Log("slotToLeftRad = "+slotToLeftRad+" slotToRightRad = "+slotToRightRad);
            SlotForward = RotateUnitVector(SlotForward,rotateRad);
        }
        else
        {
            if (Mathf.Abs(slotToLeftRad) <= 0.1f)
            {
                SlotForward = RotateUnitVector(SlotForward,-0.1f);
            }
            if (Mathf.Abs(slotToRightRad) <= 0.1f)
            {
                SlotForward = RotateUnitVector(SlotForward,0.1f);
            }    
        }
        // float tmp = Vector3.Dot(slotVec,transform.forward);
        // if(tmp < 0) tmp += Mathf.PI;
        // if (tmp < slotToRightDeg || tmp > slotToLeftRad) 
        // Debug.Log(name+" tmp = "+tmp + " slotToLeftRad "+slotToLeftRad+" slotToRightDeg "+slotToRightDeg);
        
    }
}
