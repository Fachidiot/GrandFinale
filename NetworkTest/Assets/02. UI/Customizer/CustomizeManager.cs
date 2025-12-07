using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;
using UnityEngine;

public class CustomizeManager : MonoBehaviour
{
    [SerializeField] private GameObject customizePanel;
    [SerializeField] private ModelCustom fModel;
    public ModelCustom FModel { set { fModel = value; } }
    [SerializeField] private ModelCustom mModel;
    public ModelCustom MModel { set { mModel = value; } }
    [SerializeField] private List<GameObject> customizePreviewOffer;

    [SerializeField] private HorizontalSelector isMaleSelector;
    [SerializeField] private HorizontalSelector headSelector;
    [SerializeField] private HorizontalSelector bodySelector;
    [SerializeField] private HorizontalSelector acce1Selector;
    [SerializeField] private HorizontalSelector acce2Selector;

    private PlayerCustomizer customizer;
    private bool isMale = true;
    public bool IsMale { get { return isMale; } }

    public ModelCustom GetCurrentModel()
    {
        return isMale ? mModel : fModel;
    }

    public void LocalPlayerSet()
    {
        // Check prefabs
        if (customizePanel == null) Debug.LogError("CustomizeManager: NetworkLocal_M prefab not assigned.");
        if (fModel == null) Debug.LogError("CustomizeManager: NetworkLocal_F prefab not assigned.");
        if (mModel == null) Debug.LogError("CustomizeManager: NetworkClient_M prefab not assigned.");
        if (isMaleSelector == null) Debug.LogError("CustomizeManager: NetworkClient_F prefab not assigned.");
        if (headSelector == null) Debug.LogError("CustomizeManager: Single_M prefab not assigned.");
        if (bodySelector == null) Debug.LogError("CustomizeManager: Single_F prefab not assigned.");
        if (acce1Selector == null) Debug.LogError("CustomizeManager: Single_F prefab not assigned.");
        if (acce2Selector == null) Debug.LogError("CustomizeManager: Single_F prefab not assigned.");

        fModel.transform.parent.GetComponent<Animator>().SetBool("Sit", true);
        mModel.transform.parent.GetComponent<Animator>().SetBool("Sit", true);

        ModelApply();
    }

    void Awake()
    {
        customizer = PlayerCustomizer.Instance;
    }

    private void ModelApply()
    {
        fModel.ApplyModelInfo(customizer.LocalFemaleModelInfo);
        mModel.ApplyModelInfo(customizer.LocalMaleModelInfo);
    }

    public void TogglePanel()
    {
        ModelApply();
        bool toggle = !customizePanel.activeSelf;

        customizePanel.SetActive(toggle);

        for (int i = 0; i < customizePreviewOffer.Count; ++i)
            customizePreviewOffer[i].SetActive(!toggle);

        fModel.transform.parent.localRotation = Quaternion.Euler(0, 180, 0);
        mModel.transform.parent.localRotation = Quaternion.Euler(0, 180, 0);

        fModel.transform.parent.GetComponent<Animator>().SetBool("Sit", !toggle);
        mModel.transform.parent.GetComponent<Animator>().SetBool("Sit", !toggle);

        if (this.isMale)
            ActiveMale();
        else
            ActiveFemale();
    }

    public void ChangeMale(int isMale)
    {
        this.isMale = isMale > 0 ? false : true;
        customizer.IsMale = this.isMale;

        if (this.isMale)
            ActiveMale();
        else
            ActiveFemale();
    }

    public void ChangeHead(int head)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.head = head;
            mModel.ChangeHead(head);
        }
        else
        {
            customizer.LocalFemaleModelInfo.head = head;
            fModel.ChangeHead(head);
        }
    }

    public void ChangeBody(int body)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.body = body;
            mModel.ChangeBody(body);
        }
        else
        {
            customizer.LocalFemaleModelInfo.body = body;
            fModel.ChangeBody(body);
        }
    }

    public void ChangeAcc1(int acc1)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.acc1 = acc1;
            mModel.ChangeAcc1(acc1);
        }
        else
        {
            customizer.LocalFemaleModelInfo.acc1 = acc1;
            fModel.ChangeAcc1(acc1);
        }
    }

    public void ChangeAcc2(int acc2)
    {
        if (isMale)
        {
            customizer.LocalMaleModelInfo.acc2 = acc2;
            mModel.ChangeAcc2(acc2);
        }
        else
        {
            customizer.LocalFemaleModelInfo.acc2 = acc2;
            fModel.ChangeAcc2(acc2);
        }
    }

    public void SaveData()
    {
        customizer.SaveModelInfo();

        // If we are a client in a network game, send an update to the host.
        if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Client)
        {
            ModelInfo localModelInfo = customizer.GetLocalPlayerInfo();
            bool isLocalMale = customizer.IsMale;

            // Create JSON message for customization
            Newtonsoft.Json.Linq.JObject customizationMessage = new Newtonsoft.Json.Linq.JObject
            {
                { "type", "player_customization" },
                { "isMale", isLocalMale },
                { "head", localModelInfo.head },
                { "body", localModelInfo.body },
                { "acc1", localModelInfo.acc1 },
                { "acc2", localModelInfo.acc2 }
            };

            // Send customization data to the host
            NetworkManager.Instance.SendJsonMessage(NetworkManager.Instance.LobbyHostID, customizationMessage);

            Debug.Log("Sent customization update to host.");
        }
        // If we are the host, update our own data directly and broadcast.
        else if (NetworkManager.Instance != null && NetworkManager.Instance.Mode == NetworkMode.Host)
        {
            ModelInfo localModelInfo = customizer.GetLocalPlayerInfo();
            bool isLocalMale = customizer.IsMale;
            ServerRoomManager.Instance.UpdateHostCustomization(localModelInfo, isLocalMale);
            Debug.Log("Host updated own customization and broadcasted.");
        }
    }

    public void ActiveFemale()
    {
        mModel.transform.parent.gameObject.SetActive(false);
        fModel.transform.parent.gameObject.SetActive(true);

        headSelector.defaultIndex = 0;
        headSelector.items.Clear();
        for (int i = 0; i < fModel.headModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("머리 " + (i + 1)).ToString();
            headSelector.items.Add(item);
        }
        headSelector.index = customizer.LocalFemaleModelInfo.head;
        headSelector.UpdateUI();

        bodySelector.defaultIndex = 0;
        bodySelector.items.Clear();
        for (int i = 0; i < fModel.bodyModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("몸통 " + (i + 1)).ToString();
            bodySelector.items.Add(item);
        }
        bodySelector.index = customizer.LocalFemaleModelInfo.body;
        bodySelector.UpdateUI();

        acce1Selector.defaultIndex = 0;
        acce1Selector.items.Clear();
        for (int i = 0; i < fModel.acc1Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구1 " + (i + 1)).ToString();
            acce1Selector.items.Add(item);
        }
        acce1Selector.index = customizer.LocalFemaleModelInfo.acc1;
        acce1Selector.UpdateUI();

        acce2Selector.defaultIndex = 0;
        acce2Selector.items.Clear();
        for (int i = 0; i < fModel.acc2Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구2 " + (i + 1)).ToString();
            acce2Selector.items.Add(item);
        }
        acce2Selector.index = customizer.LocalFemaleModelInfo.acc2;
        acce2Selector.UpdateUI();
    }

    public void ActiveMale()
    {
        mModel.transform.parent.gameObject.SetActive(true);
        fModel.transform.parent.gameObject.SetActive(false);

        headSelector.defaultIndex = 0;
        headSelector.items.Clear();
        for (int i = 0; i < mModel.headModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("머리 " + (i + 1)).ToString();
            headSelector.items.Add(item);
        }
        headSelector.index = customizer.LocalMaleModelInfo.head;
        headSelector.UpdateUI();

        bodySelector.defaultIndex = 0;
        bodySelector.items.Clear();
        for (int i = 0; i < mModel.bodyModels.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("몸통 " + (i + 1)).ToString();
            bodySelector.items.Add(item);
        }
        bodySelector.index = customizer.LocalMaleModelInfo.body;
        bodySelector.UpdateUI();

        acce1Selector.defaultIndex = 0;
        acce1Selector.items.Clear();
        for (int i = 0; i < mModel.acc1Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구1 " + (i + 1)).ToString();
            acce1Selector.items.Add(item);
        }
        acce1Selector.index = customizer.LocalMaleModelInfo.acc1;
        acce1Selector.UpdateUI();

        acce2Selector.defaultIndex = 0;
        acce2Selector.items.Clear();
        for (int i = 0; i < mModel.acc2Models.Count; ++i)
        {
            HorizontalSelector.Item item = new HorizontalSelector.Item();
            item.itemTitle = ("장신구2 " + (i + 1)).ToString();
            acce2Selector.items.Add(item);
        }
        acce2Selector.index = customizer.LocalMaleModelInfo.acc2;
        acce2Selector.UpdateUI();
    }
}
