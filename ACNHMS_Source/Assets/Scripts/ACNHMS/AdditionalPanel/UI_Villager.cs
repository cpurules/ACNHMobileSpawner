﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NHSE.Core;
using NH_CreationEngine;


public class UI_Villager : IUI_Additional
{
    const int VillagersSize = Villager2.SIZE * 10;
    const int VillagerHousesSize = VillagerHouse2.SIZE * 10;

    // sprites
    public static string VillagerFilename = "villagerdump3";
    public static string VillagerFilenameHeader = VillagerFilename + "header";
    public static string VillagerPath { get { return SpriteBehaviour.UsableImagePath + Path.DirectorySeparatorChar + VillagerFilename; } }
    public static string VillagerHeaderPath { get { return SpriteBehaviour.UsableImagePath + Path.DirectorySeparatorChar + VillagerFilenameHeader; } }

    public static string VillagerRootAddress = OffsetHelper.VillagerAddress.ToString("X"); 
    public static string VillagerHouseAddress = OffsetHelper.VillagerHouseAddress.ToString("X");
    public static uint CurrentVillagerAddress { get { return StringUtil.GetHexValue(VillagerRootAddress); } }
    public static uint CurrentVillagerHouseAddress { get { return StringUtil.GetHexValue(VillagerHouseAddress); } }

    public Text VillagerName, SaveVillagerLabel, PlayerNameGSave;
    public RawImage MainVillagerTexture;
    public InputField VillagerPhrase, VillagerFriendship;
    public Toggle MovingOutToggle, ReloadVillagerToggle, ForceMoveOutToggle;
    public InputField VillagerRamOffset, VillagerHouseRamOffset;
    public Button DataButton;

    public RawImage[] TenVillagers;

    public GameObject BlockerRoot;

    public UI_VillagerSelect Selector;
    public UI_VillagerData DataSelector;

    private Villager2 loadedVillager;
    private List<VillagerHouse2> loadedVillagerHouses;
    private List<Villager2> loadedVillagerShellsList;
    private SpriteParser villagerSprites;
    private bool loadedVillagerShells = false;
    private int currentlyLoadedVillagerIndex = -1;

    private int currentSelectedGSaveMemory = 0;

    public VillagerHouse2 GetCurrentLoadedVillagerHouse() => loadedVillagerHouses?.Find(x => x.NPC1 == (sbyte)currentlyLoadedVillagerIndex);

    public Villager2 GetCurrentlyLoadedVillager() => loadedVillager;

    void Start()
    {
        checkAndLoadSpriteDump();
        VillagerRamOffset.text = VillagerRootAddress;
        VillagerHouseRamOffset.text = VillagerHouseAddress;

        for (int i = 0; i < TenVillagers.Length; ++i)
        {
            int tmpVal = i; // non indexed so it doesn't screw up 
            TenVillagers[i].GetComponent<Button>().onClick.AddListener(delegate { LoadVillager(tmpVal); });
        }

        VillagerPhrase.onValueChanged.AddListener(delegate { loadedVillager.CatchPhrase = VillagerPhrase.text; });
        VillagerFriendship.onValueChanged.AddListener(delegate { VillagerFriendship.text = setCurrentPlayerFriendship(int.Parse(VillagerFriendship.text)).ToString(); });
        MovingOutToggle.onValueChanged.AddListener(delegate {
            ushort[] flags = loadedVillager.GetEventFlagsSave();
            flags[5] = 1; // flag 5 = MoveInCompletion
            flags[9] = 0; // flag 9 = AbandonedHouse
            loadedVillager.SetEventFlagsSave(flags);
            loadedVillager.MovingOut = MovingOutToggle.isOn;
        });
        ForceMoveOutToggle.onValueChanged.AddListener(delegate {
            if (ForceMoveOutToggle.isOn)
                loadedVillager.MovingOut = true;
            ushort[] flags = loadedVillager.GetEventFlagsSave();
            flags[24] = ForceMoveOutToggle.isOn ? (ushort)1 : (ushort)0; // flag 24 = ForceMoveOut
            flags[5] = 1; // flag 5 = MoveInCompletion
            flags[9] = 0; // flag 9 = AbandonedHouse
            loadedVillager.SetEventFlagsSave(flags);
        });

        VillagerRamOffset.onValueChanged.AddListener(delegate { VillagerRootAddress = VillagerRamOffset.text; });
        VillagerHouseRamOffset.onValueChanged.AddListener(delegate { VillagerHouseAddress = VillagerHouseRamOffset.text; });

        DataButton.interactable = false;
    }

    int setCurrentPlayerFriendship(int nVal)
    {
        if (nVal > byte.MaxValue)
            nVal = byte.MaxValue;
        var mem = loadedVillager.GetMemory(currentSelectedGSaveMemory);
        mem.Friendship = (byte)nVal;
        loadedVillager.SetMemory(mem, currentSelectedGSaveMemory);
        return nVal;
    }

    private void loadAllVillagers()
    {
        try
        {
            // load all houses
            loadAllHouses();

            loadedVillagerShellsList = new List<Villager2>();
            for (int i = 0; i < 10; ++i)
            {
                byte[] loaded = CurrentConnection.ReadBytes(CurrentVillagerAddress + (uint)(i * Villager2.SIZE), 3);
                Villager2 villagerShell = new Villager2(loaded);
                loadedVillagerShellsList.Add(villagerShell);
                if (villagerShell.Species == (byte)VillagerSpecies.non)
                {
                    Texture2D pic = ResourceLoader.GetTreeImage();
                    TenVillagers[i].texture = pic;
                }
                else
                {
                    Texture2D pic = SpriteBehaviour.PullTextureFromParser(villagerSprites, villagerShell.InternalName);
                    if (pic != null)
                        TenVillagers[i].texture = pic;
                }
            }

            loadedVillagerShells = true;
            BlockerRoot.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            PopupHelper.CreateError(e.Message, 2f);
        }
    }

    public void IncrementCurrentVillagerMemory(bool decrement)
    {
        int toAdd = decrement ? -1 : 1;
        int lastIndex = currentSelectedGSaveMemory; // bc editing text causes the onvaluechanged call
        currentSelectedGSaveMemory = mod(currentSelectedGSaveMemory + toAdd , Villager2.PlayerMemoryCount);
        try
        {
            var mem = loadedVillager.GetMemory(currentSelectedGSaveMemory);
            PlayerNameGSave.text = mem.PlayerName;
            VillagerFriendship.text = mem.Friendship.ToString();
            PlayerNameGSave.color = Color.white;
            if (PlayerNameGSave.text == "")
                PlayerNameGSave.text = string.Format("<no-one ({0})>", currentSelectedGSaveMemory);
        }
        catch { PlayerNameGSave.color = Color.red; currentSelectedGSaveMemory = lastIndex; }
    }

    private void loadAllHouses()
    {
        loadedVillagerHouses = new List<VillagerHouse2>();
        byte[] houses = CurrentConnection.ReadBytes(CurrentVillagerHouseAddress, VillagerHouse2.SIZE * 10);
        for (int i = 0; i < 10; ++i)
        {
            loadedVillagerHouses.Add(new VillagerHouse2(houses.Slice(i * VillagerHouse2.SIZE, VillagerHouse2.SIZE)));
        }
    }

    public void LoadAllVillagers() // gets first 3 bytes of each villager
    {
        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f, "Loading villagers...", () => { loadAllVillagers(); });
    }

    public void loadVillager(int index)
    {
        try
        {
            byte[] loaded = CurrentConnection.ReadBytes(CurrentVillagerAddress + (uint)(index * Villager2.SIZE), Villager2.SIZE);

            if (villagerIsNull(loaded))
                return;

            // reload all houses
            loadAllHouses();

            currentlyLoadedVillagerIndex = index;
            loadedVillager = new Villager2(loaded);

            VillagerToUI(loadedVillager);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            PopupHelper.CreateError(e.Message, 2f);
        }
    }

    private Villager2 loadVillagerExternal(int index, bool includeHouses)
    {
        try
        {
            byte[] loaded = CurrentConnection.ReadBytes(CurrentVillagerAddress + (uint)(index * Villager2.SIZE), Villager2.SIZE);

            if (villagerIsNull(loaded))
                return null;

            // reload all houses
            if (includeHouses)
                loadAllHouses();
            
            return new Villager2(loaded);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            PopupHelper.CreateError(e.Message, 2f);
            return null;
        }
    }

    public void LoadVillager(int index)
    {
        if (!loadedVillagerShells)
            return;

        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f, 
            string.Format("Fetching {0}...", GameInfo.Strings.GetVillager(loadedVillagerShellsList[index].InternalName)), 
            () => { loadVillager(index); }, 
            loadedVillagerShellsList[index].Gender == 0 ? new Color (0.15f, 0.46f, 1f) : new Color (1f, 0.51f, 0.75f), // blue or pink
            false,
            (Texture2D)TenVillagers[index].texture); 
    }

    public void VillagerToUI(Villager2 v)
    {
        currentSelectedGSaveMemory = 0;
        var mem = v.GetMemory(currentSelectedGSaveMemory);
        VillagerName.text = GameInfo.Strings.GetVillager(v.InternalName);
        VillagerPhrase.text = v.CatchPhrase;
        VillagerFriendship.text = mem.Friendship.ToString();
        PlayerNameGSave.text = mem.PlayerName;
        PlayerNameGSave.color = Color.white;
        MainVillagerTexture.texture = SpriteBehaviour.PullTextureFromParser(villagerSprites, v.InternalName);
        MovingOutToggle.isOn = v.MovingOut;
        ForceMoveOutToggle.isOn = v.GetEventFlagsSave()[24] != 0;

        SaveVillagerLabel.text = string.Format("Save villager ({0})", VillagerName.text);

        DataButton.interactable = true;
    }

    private void setCurrentVillager(bool includeHouse)
    {
        if (currentlyLoadedVillagerIndex == -1)
            return;

        // force AbandonedHouse = 0
        ushort[] flags = loadedVillager.GetEventFlagsSave();
        flags[9] = (ushort)0;
        loadedVillager.SetEventFlagsSave(flags);

        try
        {
            byte[] villager = loadedVillager.Data;
            CurrentConnection.WriteBytes(villager, CurrentVillagerAddress + (uint)(currentlyLoadedVillagerIndex * Villager2.SIZE));

            if (includeHouse)
            {
                // send all houses
                List<byte> linearHouseArray = new List<byte>();
                foreach (VillagerHouse2 vh in loadedVillagerHouses)
                    linearHouseArray.AddRange(vh.Data);
                CurrentConnection.WriteBytes(linearHouseArray.ToArray(), CurrentVillagerHouseAddress);
                CurrentConnection.WriteBytes(linearHouseArray.ToArray(), CurrentVillagerHouseAddress + (uint)OffsetHelper.BackupSaveDiff); // there's a temporary day buffer
            }


            if (UI_ACItemGrid.LastInstanceOfItemGrid != null)
                UI_ACItemGrid.LastInstanceOfItemGrid.PlayHappyParticles();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            PopupHelper.CreateError(e.Message, 2f);
        }
    }

    public void SetCurrentVillagerWithCheck()
    {
        if (currentlyLoadedVillagerIndex == -1)
        {
            PopupHelper.CreateError("No villager selected. Select a villager from the left-hand panel.", 2f);
            return;
        }
        checkReloadVillager();
        setCurrentVillager();
    }

    private void setCurrentVillager()
    {
        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f,
            string.Format("Saving {0}...", GameInfo.Strings.GetVillager(loadedVillagerShellsList[currentlyLoadedVillagerIndex].InternalName)), 
            () => { setCurrentVillager(true); },
            loadedVillagerShellsList[currentlyLoadedVillagerIndex].Gender == 0 ? new Color(0.15f, 0.46f, 1f) : new Color(1f, 0.51f, 0.75f), // blue or pink
            false,
            (Texture2D)TenVillagers[currentlyLoadedVillagerIndex].texture);
    }

    public void RevertCurrentPhraseToOriginal()
    {
        if (currentlyLoadedVillagerIndex == -1)
            return;

        VillagerPhrase.text = loadedVillager.CatchPhrase = GameInfo.Strings.GetVillagerDefaultPhrase(loadedVillager.InternalName);
    }

    public void ShowSelector()
    {
        Selector.Init(() => { loadVillagerFromResource(); }, () => { }, villagerSprites);
    }

    public void ShowDataSelector()
    {
        DataSelector.gameObject.SetActive(true);
        DataSelector.VillagerName.text = VillagerName.text;
        DataSelector.VillagerImg.texture = (Texture2D)MainVillagerTexture.texture;
    }

    private void loadVillagerFromResource()
    {
        if (currentlyLoadedVillagerIndex == -1)
        {
            PopupHelper.CreateError("No villager selected to replace.", 2f);
            return;
        }
        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f,
            string.Format("Sending {0}...", GameInfo.Strings.GetVillager(loadedVillagerShellsList[currentlyLoadedVillagerIndex].InternalName)), 
            () => { loadVillagerDataFromSelector(); },
            null,
            false,
            (Texture2D)TenVillagers[currentlyLoadedVillagerIndex].texture);
    }

    private void loadVillagerDataFromSelector()
    {
        try
        {
            checkReloadVillager();
            string newVillager = Selector.LastSelectedVillager;
            byte[] villagerDump = ((TextAsset)Resources.Load("DefaultVillagers/" + newVillager + "V")).bytes;
            byte[] villagerHouse = ((TextAsset)Resources.Load("DefaultVillagers/" + newVillager + "H")).bytes;
            if (villagerDump == null || villagerHouse == null)
                throw new Exception("Villager not found: " + newVillager);

            // force replaced villager to be moving out
            var villager = new Villager2(villagerDump);
            villager.MovingOut = true;
            loadVillagerData(villager, new VillagerHouse2(villagerHouse));
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            PopupHelper.CreateError(e.Message, 2f);
        }
    }

    private void loadVillagerData(Villager2 v, VillagerHouse2 vh, bool raw = false)
    {
        try
        {
            Villager2 newV = v;
            VillagerHouse2 newVH = vh;
            VillagerHouse2 loadedVillagerHouse = GetCurrentLoadedVillagerHouse(); // non indexed so search for the correct one
            int index = loadedVillagerHouses.IndexOf(loadedVillagerHouse);
            if (!raw && index != -1)
            {
                newV.SetMemories(loadedVillager.GetMemories());
                newV.SetEventFlagsSave(loadedVillager.GetEventFlagsSave());
                newV.CatchPhrase = GameInfo.Strings.GetVillagerDefaultPhrase(newV.InternalName);
            }
            
            if (index == -1)
            {
                //inject to earliest available house
                foreach (var house in loadedVillagerHouses)
                {
                    if (house.NPC1 == -1)
                    {
                        loadedVillagerHouse = house;
                        index = loadedVillagerHouses.IndexOf(loadedVillagerHouse);
                        house.NPC1 = (sbyte)currentlyLoadedVillagerIndex;
                        break;
                    }
                }

                if (index == -1)
                    throw new Exception("Selected villager has no house, and no houses are available.");
            }

            // check if they are moving in
            if (loadedVillagerHouse.WallUniqueID == WallType.HouseWallNForSale || loadedVillagerHouse.WallUniqueID == WallType.HouseWallNSoldOut)
                loadedVillagerHouse.WallUniqueID = newVH.WallUniqueID;
            if (checkIfMovingIn(loadedVillagerHouse))
                newVH = combineHouseOrders(newVH, loadedVillagerHouse);
            newVH.NPC1 = loadedVillagerHouse.NPC1;

            loadedVillagerHouses[index] = newVH;
            loadedVillager = newV;
            loadedVillagerShellsList[currentlyLoadedVillagerIndex] = newV;

            TenVillagers[currentlyLoadedVillagerIndex].texture = SpriteBehaviour.PullTextureFromParser(villagerSprites, newV.InternalName);
            setCurrentVillager(); // where the magic happens
            VillagerToUI(loadedVillager);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            PopupHelper.CreateError(e.Message, 2f);
        }
    }

    private bool checkIfMovingIn(VillagerHouse2 vOld) => vOld.WallUniqueID == WallType.HouseWallNSoldOut;

    private VillagerHouse2 combineHouseOrders(VillagerHouse2 vNew, VillagerHouse2 vOld)
    {
        VillagerHouse2 vTmp = new VillagerHouse2(vOld.Data);
        vTmp.OrderWallUniqueID = vNew.OrderWallUniqueID;
        vTmp.OrderRoofUniqueID = vNew.OrderRoofUniqueID;
        vTmp.OrderDoorUniqueID = vNew.OrderDoorUniqueID;
        return vTmp;
    }

    private void checkReloadVillager()
    {
        if (ReloadVillagerToggle.isOn)
        {
            Villager2 v = loadVillagerExternal(currentlyLoadedVillagerIndex, true);
            if (v != null)
            {
                loadedVillager.SetMemories(v.GetMemories());
                loadedVillager.SetEventFlagsSave(v.GetEventFlagsSave());
                loadedVillager.MovingOut = v.MovingOut;
            }
        }

    }

    // villager data

    public void WriteVillagerDataHouse(VillagerHouse2 vh)
    {
        checkReloadVillager();
        loadVillagerData(loadedVillager, vh, true);
    }

    public void WriteVillagerDataVillager(Villager2 v)
    {
        checkReloadVillager();
        VillagerHouse2 loadedVillagerHouse = loadedVillagerHouses.Find(x => x.NPC1 == (sbyte)currentlyLoadedVillagerIndex); // non indexed so search for the correct one
        int index = loadedVillagerHouses.IndexOf(loadedVillagerHouse);
        if (index == -1)
            throw new Exception("The villager having their house replaced doesn't have a house on your island."); // not sure why but it can get unloaded during the check

        loadVillagerData(v, loadedVillagerHouse, true);
    }

    public void DumpVillagerArray()
    {
        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f, "Fetching all villager data, this may take a long time...", () =>
        {
            var villagerBytes = CurrentConnection.ReadBytes(CurrentVillagerAddress, VillagersSize);
            var villagerHouseBytes = CurrentConnection.ReadBytes(CurrentVillagerHouseAddress, VillagerHousesSize);
            var combined = villagerBytes.Concat(villagerHouseBytes).ToArray();

            string names = string.Empty;
            // get names
            for (int i = 0; i < 10; ++i)
            {
                var species = (VillagerSpecies)villagerBytes[i * Villager2.SIZE];
                if (species != VillagerSpecies.non)
                {
                    var variant = villagerBytes[(i * Villager2.SIZE) + 1];
                    var intern = VillagerUtil.GetInternalVillagerName(species, variant);
                    names += $"_{GameInfo.Strings.GetVillager(intern)}";
                }
                else
                    names += $"_EMPTY";
            }

            UI_NFSOACNHHandler.LastInstanceOfNFSO.SaveFile($"{names}.bin", combined);
        });
    }

    public void LoadVillagerArray()
    {
        var sizeExpected = VillagersSize + VillagerHousesSize;
        UI_NFSOACNHHandler.LastInstanceOfNFSO.OpenAnyFile(loadVillagerBytes, sizeExpected);
    }

    private void loadVillagerBytes(byte[] bytes)
    {
        var villagerBytes = bytes.Take(VillagersSize).ToArray();
        var villagerHouseBytes = bytes.Skip(VillagersSize).ToArray();
        UI_Popup.CurrentInstance.CreatePopupMessage(0.001f, "Sending all villager data, this may take a long time...", () =>
        {
            CurrentConnection.WriteBytes(villagerBytes, CurrentVillagerAddress);
            CurrentConnection.WriteBytes(villagerHouseBytes, CurrentVillagerHouseAddress);
        });
    }

    // tools

    private void checkAndLoadSpriteDump()
    {
        string dir = Path.GetDirectoryName(VillagerPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if(!File.Exists(VillagerPath))
        {
            byte[] byteDump = ((TextAsset)Resources.Load("SpriteLoading/" + VillagerFilename)).bytes;
            byte[] byteHeader = ((TextAsset)Resources.Load("SpriteLoading/" + VillagerFilenameHeader)).bytes;
            File.WriteAllBytes(VillagerPath, byteDump);
            File.WriteAllBytes(VillagerHeaderPath, byteHeader);
        }

        villagerSprites = new SpriteParser(VillagerPath, VillagerHeaderPath);
    }

    private bool villagerIsNull(byte[] villager) // first 32 bytes will be 0
    {
        int maxCheck = Mathf.Min(villager.Length, 32);
        for (int i = 0; i < maxCheck; ++i)
            if (villager[i] != 0)
                return false;
        return true;
    }

    private int mod(int x, int m) // negative-safe
    {
        return (x % m + m) % m;
    }
}
