using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }
    public GameObject notification;
    public TextMeshProUGUI notificationMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public async void ShowNotification(string message)
    { 
        notificationMessage.text = message;
        notification.SetActive(true);
        await Task.Delay(5000);
        notification.SetActive(false);
    }
}
