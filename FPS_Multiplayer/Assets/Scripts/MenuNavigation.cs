using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion; // Bắt buộc phải có để Unity hiểu NetworkRunner là gì

public class MenuNavigation : MonoBehaviour
{
    public void BackToMainMenu()
    {
        // 1. Kiểm tra và ngắt kết nối mạng
        if (NetworkRunner.Instances.Count > 0)
        {
            foreach (var runner in NetworkRunner.Instances)
            {
                // Tắt kết nối và giải phóng bộ nhớ của Fusion
                runner.Shutdown();
            }
        }

        // 2. Chuyển về Scene Menu chính
        SceneManager.LoadScene("MainMenuScene");
    }
}   