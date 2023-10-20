using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Suinet.SuiPlay.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public TMP_InputField signInEmail;
    public TMP_InputField signInPassword;
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;
    public TMP_InputField signUpDisplayName;
    public GameObject loading;
    public GameObject SigningPanel;
    public TextMeshProUGUI username;
    public TextMeshProUGUI walletAddress;
    public TextMeshProUGUI coinBalance;
    public GameObject MainPanel;
    private bool requiresAirdrop;

    private void Awake()
    {
        if (WalletManager.Instance.player == null) SigningPanel.SetActive(true);
        else SetUpMainMenu();
    }

    public async void SignIn()
    {
        loading.SetActive(true);
        var registrationRequest = new LoginRequest()
        {
            Email = signInEmail.text,
            Password = signInPassword.text,
            GameId = SuiPlayConfig.GAME_ID,
            StudioId = SuiPlayConfig.STUDIO_ID
        };

        var result = await SuiPlay.Client.LoginWithEmailAsync(registrationRequest);
        Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        if (result.IsSuccess)
        {
            GetUserProfile();
        }
    }

    public async void SignUp()
    {
        loading.SetActive(true);
        var registrationRequest = new RegistrationRequest()
        {
            Email = signUpEmail.text,
            Password = signUpPassword.text,
            DisplayName = signUpDisplayName.text,
            GameId = SuiPlayConfig.GAME_ID,
            StudioId = SuiPlayConfig.STUDIO_ID
        };

        var result = await SuiPlay.Client.RegisterWithEmailAsync(registrationRequest);
        Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        if (result.IsSuccess)
        {
            requiresAirdrop = true;
            loading.SetActive(false);
            NotificationManager.Instance.ShowNotification(result.Value.Message);
        }
    }

    private async void GetUserProfile()
    {
        var result = await SuiPlay.Client.GetPlayerProfileAsync(SuiPlayConfig.GAME_ID);
        WalletManager.Instance.SetPlayer(result.Value);
        Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        loading.SetActive(false);

        if (requiresAirdrop)
        {
            requiresAirdrop = false;
            await SuiAirdrop.RequestAirdrop(WalletManager.Instance.player.Wallets.First().Value.Address);
        }

        SetUpMainMenu();
    }

    private async void SetUpMainMenu()
    {
        coinBalance.text = $"SUI: {await WalletManager.Instance.GetUserSuiBalance()}";
        username.text = WalletManager.Instance.player.DisplayName;
        walletAddress.text = WalletManager.Instance.player.Wallets.First().Value.Address;
        SigningPanel.SetActive(false);
        MainPanel.SetActive(true);
        loading.SetActive(false);
    }

    public void Play()
    {
        SceneManager.LoadScene("PersistentScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}