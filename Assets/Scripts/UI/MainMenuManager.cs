using System;
using System.Linq;
using SimpleJSON;
using Newtonsoft.Json;
using Suinet.Rpc;
using Suinet.Rpc.Types;
using Suinet.SuiPlay.Requests;
using Suinet.Wallet;
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

        if (requiresAirdrop)
        {
            requiresAirdrop = false;
            await SuiAirdrop.RequestAirdrop(WalletManager.Instance.player.Wallets.First().Value.Address);
        }

        GetUserMetadata();
    }

    private async void GetUserMetadata()
    {
        var address = SuiWallet.GetActiveAddress();
        var filter = ObjectDataFilterFactory.CreateMatchAllFilter(ObjectDataFilterFactory.CreateAddressOwnerFilter(address));
        var ownedObjectsResult = await SuiApi.Client.GetOwnedObjectsAsync(address, new ObjectResponseQuery() { Filter = filter, Options = ObjectDataOptions.ShowAll() }, null, null);
        Debug.Log(JsonConvert.SerializeObject(ownedObjectsResult.Result, Formatting.Indented));

        for (int i = 0; i < ownedObjectsResult.Result.Data.Count; i++)
        {
            if (((MoveObjectData)ownedObjectsResult.Result.Data[i].Data.Content).Type == "0x0e4eac7bdfb5400e7f3dca1166290479017cc64ab67a2a6fee54708b93a3a1e2::game::Player")
            {
                WalletManager.Instance.user = JSON.Parse(JsonConvert.SerializeObject(((MoveObjectData)ownedObjectsResult.Result.Data[i].Data.Content).Fields));

                SetUpMainMenu();
                return;
            }
        }

        CreateUserMetadata();
    }

    private async void CreateUserMetadata()
    {
        var signer = SuiWallet.GetActiveAddress();
        var moveCallTx = new MoveCallTransaction()
        {
            Signer = signer,
            PackageObjectId = "0x0e4eac7bdfb5400e7f3dca1166290479017cc64ab67a2a6fee54708b93a3a1e2",
            Module = "game",
            Function = "create_player",
            TypeArguments = ArgumentBuilder.BuildTypeArguments(),
            Arguments = ArgumentBuilder.BuildArguments(0),
            Gas = null,
            GasBudget = 100000000,
            RequestType = ExecuteTransactionRequestType.WaitForLocalExecution
        };

        var moveCallResult = await SuiApi.Client.MoveCallAsync(moveCallTx);

        var txBytes = moveCallResult.Result.TxBytes;
        var rawSigner = new RawSigner(SuiWallet.GetActiveKeyPair());
        var signature = rawSigner.SignData(Intent.GetMessageWithIntent(txBytes));

        var txResponse = await SuiApi.Client.ExecuteTransactionBlockAsync(txBytes, new[] { signature.Value }, TransactionBlockResponseOptions.ShowAll(), ExecuteTransactionRequestType.WaitForLocalExecution);

        if (!txResponse.IsSuccess || txResponse.Result.Effects.Status.Status == ExecutionStatus.Failure)
        {
            NotificationManager.Instance.ShowNotification(txResponse.ErrorMessage);
        }
        else
        {
            SetUpMainMenu();
        }
        
        Debug.Log(txResponse.ErrorMessage);
        Debug.Log(JsonConvert.SerializeObject(txResponse.Result, Formatting.Indented));
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