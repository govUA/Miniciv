using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class CivilizationEntry
    {
        public string civName;
    }

    [System.Serializable]
    public class CivilizationList
    {
        public List<CivilizationEntry> civilizations;
    }

    [Header("Game and players")] public TMP_Dropdown civilizationDropdown;
    public Slider playerCountSlider;
    public TextMeshProUGUI playerCountText;
    public TextAsset civilizationsJson;

    [Header("Map settings")] public TMP_Dropdown mapSizeDropdown;
    public TMP_Dropdown mapTypeDropdown;
    public TMP_Dropdown seaLevelDropdown;
    public Toggle wrapWorldToggle;

    [Header("Custom sized map")] public GameObject customSizePanel;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;

    [Header("Custom Sea Level Settings")] public GameObject customSeaLevelPanel;
    public TMP_InputField smoothingIterationsInput;
    public TMP_InputField fillPercentInput;

    [Header("Generation (Seed)")] public Toggle useRandomSeedToggle;
    public TMP_InputField seedInput;

    private void Start()
    {
        PopulateDropdowns();

        if (useRandomSeedToggle != null && seedInput != null)
        {
            useRandomSeedToggle.onValueChanged.AddListener(OnRandomSeedToggleChanged);
            OnRandomSeedToggleChanged(useRandomSeedToggle.isOn);
        }

        if (playerCountSlider != null)
        {
            playerCountSlider.wholeNumbers = true;
            playerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
        }

        if (mapSizeDropdown != null)
        {
            mapSizeDropdown.onValueChanged.AddListener(OnMapSizeChanged);
            OnMapSizeChanged(mapSizeDropdown.value);
        }

        if (seaLevelDropdown != null)
        {
            seaLevelDropdown.onValueChanged.AddListener(OnSeaLevelChanged);
            OnSeaLevelChanged(seaLevelDropdown.value);
        }
    }

    private void OnMapSizeChanged(int index)
    {
        if (customSizePanel != null)
        {
            customSizePanel.SetActive(index == 5);
        }

        UpdatePlayerCountLimits(index);
    }

    private void OnSeaLevelChanged(int index)
    {
        if (customSeaLevelPanel != null)
        {
            customSeaLevelPanel.SetActive(index == 3);
        }
    }

    private int GetClampedValue(TMP_InputField input, int min, int max, int defaultValue)
    {
        if (input != null && int.TryParse(input.text, out int val))
        {
            return Mathf.Clamp(val, min, max);
        }

        return defaultValue;
    }

    private void UpdatePlayerCountLimits(int mapSizeIndex)
    {
        if (playerCountSlider == null) return;

        int minPlayers = 2;
        int maxPlayers = 8;

        switch (mapSizeIndex)
        {
            case 0:
                maxPlayers = 4;
                break;
            case 1:
                maxPlayers = 6;
                break;
            case 2:
                maxPlayers = 8;
                break;
            case 3:
                maxPlayers = 10;
                break;
            case 4:
                maxPlayers = 12;
                break;
            case 5:
                maxPlayers = 16;
                break;
        }

        playerCountSlider.minValue = minPlayers;
        playerCountSlider.maxValue = maxPlayers;

        if (playerCountSlider.value > maxPlayers)
            playerCountSlider.value = maxPlayers;
        if (playerCountSlider.value < minPlayers)
            playerCountSlider.value = minPlayers;

        OnPlayerCountChanged(playerCountSlider.value);
    }

    private void OnPlayerCountChanged(float value)
    {
        if (playerCountText != null)
        {
            playerCountText.text = value.ToString();
        }
    }

    private void OnRandomSeedToggleChanged(bool isRandom)
    {
        seedInput.interactable = !isRandom;
    }

    public void OnStartGameButtonClicked()
    {
        if (civilizationDropdown != null)
            GameSettings.PlayerCivilizationIndex = civilizationDropdown.value;

        if (playerCountSlider != null)
            GameSettings.PlayerCount = (int)playerCountSlider.value;

        if (mapSizeDropdown != null)
            GameSettings.MapSizeIndex = mapSizeDropdown.value;

        if (GameSettings.MapSizeIndex == 5)
        {
            GameSettings.CustomWidth = GetClampedValue(widthInput, 16, 256, 100);
            GameSettings.CustomHeight = GetClampedValue(heightInput, 16, 256, 60);
        }

        if (mapTypeDropdown != null)
            GameSettings.MapTypeIndex = mapTypeDropdown.value;

        if (seaLevelDropdown != null)
        {
            GameSettings.SeaLevelIndex = seaLevelDropdown.value;
            if (GameSettings.SeaLevelIndex == 3)
            {
                GameSettings.SmoothingIterations = GetClampedValue(smoothingIterationsInput, 0, 100, 3);
                GameSettings.FillPercent = GetClampedValue(fillPercentInput, 0, 100, 45);
            }
        }

        if (wrapWorldToggle != null)
            GameSettings.WrapWorld = wrapWorldToggle.isOn;

        if (useRandomSeedToggle != null)
            GameSettings.UseRandomSeed = useRandomSeedToggle.isOn;

        if (seedInput != null && !GameSettings.UseRandomSeed)
            GameSettings.Seed = seedInput.text;

        SceneManager.LoadScene("GameScene");
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void SetupDropdownFromEnum<T>(TMP_Dropdown dropdown, bool addSpaces) where T : Enum
    {
        dropdown.ClearOptions();
        List<string> names = Enum.GetNames(typeof(T)).ToList();

        if (addSpaces)
        {
            for (int i = 0; i < names.Count; i++)
            {
                names[i] = Regex.Replace(names[i], "([a-z])([A-Z])", "$1 $2");
            }
        }

        dropdown.AddOptions(names);
    }

    private void PopulateDropdowns()
    {
        if (mapSizeDropdown != null) SetupDropdownFromEnum<MapGenerator.MapSizeType>(mapSizeDropdown, false);
        if (seaLevelDropdown != null) SetupDropdownFromEnum<MapGenerator.SeaLevelType>(seaLevelDropdown, false);
        if (mapTypeDropdown != null) SetupDropdownFromEnum<MapType>(mapTypeDropdown, true);
        if (civilizationDropdown != null) LoadCivilizations();
    }

    private void LoadCivilizations()
    {
        if (civilizationsJson == null) return;

        civilizationDropdown.ClearOptions();

        CivilizationList data = JsonUtility.FromJson<CivilizationList>(civilizationsJson.text);

        List<string> options = new List<string>();
        foreach (var civ in data.civilizations)
        {
            options.Add(civ.civName);
        }

        civilizationDropdown.AddOptions(options);
    }
}