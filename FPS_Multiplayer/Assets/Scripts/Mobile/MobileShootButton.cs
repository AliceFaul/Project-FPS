using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileShootButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public StarterAssetsInputs starterAssetsInputs;

    public void OnPointerDown(PointerEventData eventData)
    {
        starterAssetsInputs.ShootInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        starterAssetsInputs.ShootInput(false);
    }
}