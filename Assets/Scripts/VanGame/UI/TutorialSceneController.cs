using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VanGame.UI
{
  /// <summary>
  /// Optional helper for the tutorial scene. Wire a Continue button to LoadMainGame().
  /// </summary>
  public class TutorialSceneController : MonoBehaviour
  {
    [SerializeField] string gameSceneName = "Main game";
    [SerializeField] Button continueButton;

    void Start()
    {
      if (continueButton != null)
        continueButton.onClick.AddListener(LoadMainGame);
    }

    public void LoadMainGame()
    {
      SceneManager.LoadScene(gameSceneName);
    }
  }
}
