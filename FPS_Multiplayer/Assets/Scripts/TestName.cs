using UnityEngine;

public class BillboardName : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Tìm camera chính trong cảnh
        mainCamera = Camera.main;
    }

    // Dùng LateUpdate để đảm bảo Camera đã di chuyển xong mới xoay tên theo
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Cách 1: Xoay mặt về phía camera (giữ nguyên hướng thẳng đứng)
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            // Đề phòng trường hợp Camera bị hủy hoặc thay đổi
            mainCamera = Camera.main;
        }
    }
}