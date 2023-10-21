using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SimpleJSON;
using UnityEngine;

public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    [HideInInspector] public Suinet.SuiPlay.DTO.Player player = null;
    [HideInInspector] public BigInteger playerSuiBalance;
    [HideInInspector] public JSONNode user;

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

    public void SetPlayer(Suinet.SuiPlay.DTO.Player player)
    {
        this.player = player;
    }

    public async Task<string> GetUserSuiBalance()
    {
        var walletAddress = player.Wallets.First().Value.Address;
        var coinBalanceResult = await SuiApi.Client.GetBalanceAsync(walletAddress, null);
        playerSuiBalance = coinBalanceResult.Result.TotalBalance;
        return coinBalanceResult.Result.TotalBalance.ToString();
    }
}