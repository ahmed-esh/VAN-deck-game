using UnityEngine;
using UnityEngine.UI;

namespace VanGame.UI
{
  public class SouvenirBackgroundAnimator : MonoBehaviour
  {
    [SerializeField] Image bg1;
    [SerializeField] Image bg2;
    [SerializeField] float switchIntervalSeconds = 1f;

    float _timer;
    bool _showFirst = true;

    void Awake()
    {
      if (bg1 == null || bg2 == null)
        ResolveBackgrounds();

      ApplyState(immediate: true);
    }

    void OnEnable()
    {
      _timer = 0f;
      ApplyState(immediate: true);
    }

    void Update()
    {
      if (bg1 == null || bg2 == null || switchIntervalSeconds <= 0f)
        return;

      _timer += Time.deltaTime;
      if (_timer < switchIntervalSeconds)
        return;

      _timer = 0f;
      _showFirst = !_showFirst;
      ApplyState(immediate: false);
    }

    void ResolveBackgrounds()
    {
      Transform root = transform;
      if (bg1 == null)
      {
        Transform found = root.Find('BG1');
        if (found != null)
          bg1 = found.GetComponent<Image>();
      }

      if (bg2 == null)
      {
        Transform found = root.Find('BG2');
        if (found != null)
          bg2 = found.GetComponent<Image>();
      }
    }

    void ApplyState(bool immediate)
    {
      if (bg1 != null)
        bg1.gameObject.SetActive(_showFirst);

      if (bg2 != null)
        bg2.gameObject.SetActive(!_showFirst);
    }
  }
}
