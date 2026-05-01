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

    [Header("Гра та Гравці")] public TMP_Dropdown civilizationDropdown;
    public TMP_InputField playerCountInput;
    public TextAsset civilizationsJson;

    [Header("Налаштування Карти")] public TMP_Dropdown mapSizeDropdown;
    public TMP_Dropdown mapTypeDropdown;
    public TMP_Dropdown seaLevelDropdown;
    public Toggle wrapWorldToggle;

    [Header("Користувацький розмір")] public GameObject customSizePanel;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;

    [Header("Генерація (Seed)")] public Toggle useRandomSeedToggle;
    public TMP_InputField seedInput;

    private void Start()
    {
        PopulateDropdowns();

        if (useRandomSeedToggle != null && seedInput != null)
        {
            useRandomSeedToggle.onValueChanged.AddListener(OnRandomSeedToggleChanged);
            OnRandomSeedToggleChanged(useRandomSeedToggle.isOn);
        }

        if (mapSizeDropdown != null)
        {
            mapSizeDropdown.onValueChanged.AddListener(OnMapSizeChanged);
            OnMapSizeChanged(mapSizeDropdown.value);
        }
    }

    private void OnMapSizeChanged(int index)
    {
        if (customSizePanel != null)
        {
            customSizePanel.SetActive(index == (int)MapGenerator.MapSizeType.Custom);
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

        if (playerCountInput != null && int.TryParse(playerCountInput.text, out int count))
            GameSettings.PlayerCount = count;

        if (mapSizeDropdown != null)
            GameSettings.MapSizeIndex = mapSizeDropdown.value;

        if (GameSettings.MapSizeIndex == 5)
        {
            if (widthInput != null && int.TryParse(widthInput.text, out int w))
                GameSettings.CustomWidth = w;

            if (heightInput != null && int.TryParse(heightInput.text, out int h))
                GameSettings.CustomHeight = h;
        }

        if (mapTypeDropdown != null)
            GameSettings.MapTypeIndex = mapTypeDropdown.value;

        if (seaLevelDropdown != null)
            GameSettings.SeaLevelIndex = seaLevelDropdown.value;

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