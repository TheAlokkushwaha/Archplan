using UnityEngine;
using UnityEngine.UI;

public class ModelSwitcher : MonoBehaviour
{
    public GameObject[] models;  // Array to hold the models
    public Button prevButton;    // Reference to the previous button
    public Button nextButton;    // Reference to the next button

    private int currentIndex = 0; // Index to track the current model

    void Start()
    {
        // Initialize by showing only the first model
        UpdateModelVisibility();

        // Add listeners to the buttons
        prevButton.onClick.AddListener(ShowPreviousModel);
        nextButton.onClick.AddListener(ShowNextModel);
    }

    // Method to show the previous model
    void ShowPreviousModel()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = models.Length - 1; // Loop back to the last model
        }
        UpdateModelVisibility();
    }

    // Method to show the next model
    void ShowNextModel()
    {
        currentIndex++;
        if (currentIndex >= models.Length)
        {
            currentIndex = 0; // Loop back to the first model
        }
        UpdateModelVisibility();
    }

    // Method to update which model is visible
    void UpdateModelVisibility()
    {
        for (int i = 0; i < models.Length; i++)
        {
            models[i].SetActive(i == currentIndex);
        }
    }
}
