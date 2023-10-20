using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Extensions;
using Firebase.Storage;
using Firebase.Crashlytics;
using UnityEngine.Networking;
using System.Linq;

public class SaveLoadManager : SingletonMonobehaviour<SaveLoadManager>
{
    public GameSave gameSave;
    public List<ISaveable> iSaveableObjectList;
    private FirebaseStorage storage;
    private StorageReference reference;

    protected override void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        base.Awake();

        iSaveableObjectList = new List<ISaveable>();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                storage = FirebaseStorage.DefaultInstance;
                reference = storage.GetReferenceFromUrl("gs://suifarm-3badb.appspot.com");
                LoadDataFromFile();
            }
            else
            {
                Debug.LogError(string.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }

    public void LoadDataFromFile()
    {
        StorageReference saveFile = reference.Child(WalletManager.Instance.player.Wallets.First().Value.Address);

        saveFile.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                StartCoroutine(DownloadSaveData(task.Result.ToString()));
            }
            else
            {
                Debug.Log(task.Exception.ToString());
            }
        });
    }

    private IEnumerator DownloadSaveData(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.Log(request.error);
        }
        else
        {
            MemoryStream memStream = new();
            BinaryFormatter binForm = new();
            memStream.Write(request.downloadHandler.data, 0, request.downloadHandler.data.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            gameSave = (GameSave)binForm.Deserialize(memStream);

            for (int i = iSaveableObjectList.Count - 1; i > -1; i--)
            {
                if (gameSave.gameObjectData.ContainsKey(iSaveableObjectList[i].ISaveableUniqueID))
                {
                    iSaveableObjectList[i].ISaveableLoad(gameSave);
                }
                else
                {
                    Component component = (Component)iSaveableObjectList[i];
                    Destroy(component.gameObject);
                }
            }
        }

        UIManager.Instance.DisablePauseMenu();
    }

    public void SaveDataToFile()
    {
        gameSave = new GameSave();

        foreach (ISaveable iSaveableObject in iSaveableObjectList)
        {
            gameSave.gameObjectData.Add(iSaveableObject.ISaveableUniqueID, iSaveableObject.ISaveableSave());
        }

        BinaryFormatter binForm = new();
        MemoryStream memStream = new();
        binForm.Serialize(memStream, gameSave);
        byte[] byteData = memStream.ToArray();

        StorageReference uploadRef = reference.Child(WalletManager.Instance.player.Wallets.First().Value.Address);
        uploadRef.PutBytesAsync(byteData).ContinueWithOnMainThread((task) => {
            if (task.IsFaulted || task.IsCanceled)
                Debug.Log(task.Exception.ToString());
            else 
                Debug.Log("File Uploaded!");

            UIManager.Instance.DisablePauseMenu();
        });
    }

    public void StoreCurrentSceneData()
    {
        foreach (ISaveable iSaveableObject in iSaveableObjectList)
        {
            iSaveableObject.ISaveableStoreScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestoreCurrentSceneData()
    {
        foreach (ISaveable iSaveableObject in iSaveableObjectList)
        {
            iSaveableObject.ISaveableRestoreScene(SceneManager.GetActiveScene().name);
        }
    }
}
