using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // We use TextMeshPro for modern Unity UI text

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject inGamePanel;
    public GameObject endPanel;

    [Header("UI Text Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI collectibleText;
    public TextMeshProUGUI endMessageText;

    [Header("Game Settings")]
    public float maxTime = 120.0f; // 2 minutes
    private float currentTime;
    private int totalCollectibles;
    private int collectedCount = 0;
    private bool isGameActive = false;

    [Header("Player References")]
    public PlayerControl playerControl;
    public GravityShiftGun gravityGun;

    private void Awake()
    {
        // Singleton pattern to easily access this script from anywhere
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Automatically find and count every cube in the scene with the tag "Collectible"
        totalCollectibles = GameObject.FindGameObjectsWithTag("Collectible").Length;

        // Setup the initial UI state
        startPanel.SetActive(true);
        inGamePanel.SetActive(false);
        endPanel.SetActive(false);

        // Turn off player scripts so they can't move behind the Start menu
        if (playerControl != null) playerControl.enabled = false;
        if (gravityGun != null) gravityGun.enabled = false;

        // Ensure the mouse cursor is visible so the player can click "Start"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!isGameActive) return;

        // Timer Logic
        currentTime -= Time.deltaTime;
        UpdateUI();

        if (currentTime <= 0)
        {
            EndGame(false, "TIME'S UP!\nYou ran out of time.");
        }
    }

    // --- BUTTON METHODS (Assigned in Inspector) ---

    public void StartGame()
    {
        isGameActive = true;
        currentTime = maxTime;
        collectedCount = 0;

        startPanel.SetActive(false);
        inGamePanel.SetActive(true);

        // Turn the player scripts back on
        if (playerControl != null) playerControl.enabled = true;
        if (gravityGun != null) gravityGun.enabled = true;

        // Hide and lock the cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateUI();
    }

    public void RestartGame()
    {
        // Reloads the current active scene to reset everything instantly
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- GAME EVENTS ---

    public void CollectibleGrabbed()
    {
        if (!isGameActive) return;

        collectedCount++;
        UpdateUI();

        if (collectedCount >= totalCollectibles)
        {
            EndGame(true, "VICTORY!\nYou collected all the cubes!");
        }
    }

    public void PlayerFellOffMap() // Called by PlayerControl script
    {
        if (!isGameActive) return;
        EndGame(false, "GAME OVER!\nYou fell into the void.");
    }

    private void EndGame(bool won, string message)
    {
        isGameActive = false;

        inGamePanel.SetActive(false);
        endPanel.SetActive(true);

        // Change the text to show Win or Lose
        endMessageText.text = message;
        endMessageText.color = won ? Color.green : Color.red;

        // Turn off player scripts
        if (playerControl != null) playerControl.enabled = false;
        if (gravityGun != null) gravityGun.enabled = false;

        // Unlock the mouse cursor so they can click "Restart"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateUI()
    {
        // Format the raw seconds into a nice Minutes:Seconds format (e.g., 1:45)
        int minutes = Mathf.FloorToInt(currentTime / 60F);
        int seconds = Mathf.FloorToInt(currentTime - minutes * 60);
        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);

        collectibleText.text = "Cubes: " + collectedCount + " / " + totalCollectibles;
    }
}