using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class WeaponAim2D_Netcode : WeaponAim2D
    {  
        protected override void RotateWeapon(Quaternion newRotation, bool forceInstant = false) {
            if (!IsOwner)
                return;
            base.RotateWeapon(newRotation, forceInstant);
        }
    } 
}